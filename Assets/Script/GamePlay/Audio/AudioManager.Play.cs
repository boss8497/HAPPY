using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GamePlay.Audio.Interface;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Script.GamePlay.Audio {
    public partial class AudioManager {
        // 자주 쓰는 오디오는 여기 계속 캐시되어 있다가 ReleaseClip/ReleaseAllClips로만 해제된다.
        // (ScreenManager._loadedScreens / ResourceClear()와 동일한 정책)
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _loadedClips = new();

        // InstanceId 기준으로 현재 재생 중인 AudioPlayer를 추적한다.
        // _activeGroups는 player가 Track()으로 인해 파괴됐을 때(player.IsAlive == false)도 그룹 카운트를
        // 안전하게 되돌리기 위한 사이드 테이블이다(IAudioPlayer.IsAlive만으로 판단하고 player의 다른 멤버는 건드리지 않기 위함).
        private readonly Dictionary<int, IAudioPlayer>   _activePlayers      = new();
        private readonly Dictionary<int, AudioGroupType> _activeGroups       = new();
        private readonly Dictionary<AudioGroupType, int> _activeCountByGroup = new();

        private readonly List<IAudioPlayer> _finishedBuffer = new();
        private readonly List<int>          _deadBuffer     = new();

        public async UniTask<AudioHandle> PlayAsync(
            string            key,
            AudioGroupType    group       = AudioGroupType.Effect,
            bool              loop        = false,
            bool              autoRelease = true,
            float             pitch       = 1f,
            bool              is3D        = false,
            Vector3?          position    = null,
            Transform         track       = null,
            CancellationToken ct          = default
        ) {
            var clip = await GetOrLoadClipAsync(key, ct);

            // MaxCount 체크는 실제로 풀 슬롯을 점유하기 직전(로드 완료 후)에 한다.
            if (IsOverLimit(group)) {
                return AudioHandle.Invalid;
            }

            var player = _audioPooling.Pop();
            var handle = player.Rent(group, key, loop);

            _mixerGroups.TryGetValue(group, out var mixerGroup);

            var source = player.Source;
            source.outputAudioMixerGroup = mixerGroup;
            source.clip                  = clip;
            source.loop                  = loop;
            // 그룹 볼륨은 AudioMixer에서 dB로 이미 적용되므로 소스 자체 볼륨은 항상 최대로 재생한다.
            source.volume       = 1f;
            source.pitch        = pitch;
            source.spatialBlend = is3D ? 1f : 0f;

            if (is3D && track != null) {
                player.Track(track);
            }
            else if (is3D && position.HasValue) {
                player.SetPosition(position.Value);
            }

            source.Play();

            _activePlayers[handle.InstanceId] = player;
            _activeGroups[handle.InstanceId]  = group;
            IncrementActiveCount(group);

            // 요청대로 단발 재생(loop=false) + autoRelease면 재생 시작 직후 Addressable 핸들만 Release한다.
            // AudioSource는 이미 clip을 참조 중이라 재생 자체는 계속 이어진다(압축 스트리밍 클립은 예외적 위험 있음, ARCHITECTURE.md 참고).
            // loop=true(BGM 등)는 재생 중 캐시를 지우면 안 되므로 autoRelease를 무시한다.
            if (loop == false && autoRelease) {
                ReleaseClipHandle(key);
            }

            return handle;
        }

        public async UniTask PlayBGM(string key, CancellationToken ct = default) {
            var clip = await GetOrLoadClipAsync(key, ct);

            _bgmSource.Stop();
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void StopBGM() {
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        public void Stop(AudioHandle handle) {
            if (handle.IsValid == false)
                return;

            if (_activePlayers.TryGetValue(handle.InstanceId, out var player) == false)
                return;

            if (player.Matches(handle) == false)
                return;

            ReturnPlayer(player);
        }

        public void StopAll(AudioGroupType group) {
            _finishedBuffer.Clear();

            foreach (var player in _activePlayers.Values) {
                if (player.IsAlive && player.Group == group) {
                    _finishedBuffer.Add(player);
                }
            }

            foreach (var player in _finishedBuffer) {
                ReturnPlayer(player);
            }
        }

        public void ReleaseClip(string key) {
            StopAllByClip(key);
            ReleaseClipHandle(key);
        }

        public void ReleaseAllClips() {
            _finishedBuffer.Clear();
            _finishedBuffer.AddRange(_activePlayers.Values);

            foreach (var player in _finishedBuffer) {
                ReturnPlayer(player);
            }

            foreach (var handle in _loadedClips.Values) {
                Addressables.Release(handle);
            }

            _loadedClips.Clear();
        }

        private void StopAllByClip(string key) {
            _finishedBuffer.Clear();

            foreach (var player in _activePlayers.Values) {
                if (player.IsAlive && player.ClipKey == key) {
                    _finishedBuffer.Add(player);
                }
            }

            foreach (var player in _finishedBuffer) {
                ReturnPlayer(player);
            }
        }

        private void ReleaseClipHandle(string key) {
            if (_loadedClips.TryGetValue(key, out var handle) == false)
                return;

            Addressables.Release(handle);
            _loadedClips.Remove(key);
        }

        private async UniTask<AudioClip> GetOrLoadClipAsync(string key, CancellationToken ct) {
            if (_loadedClips.TryGetValue(key, out var cached)) {
                return cached.Result;
            }

            var loadHandle = Addressables.LoadAssetAsync<AudioClip>(key);

            while (!loadHandle.IsDone) {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (loadHandle.Status != AsyncOperationStatus.Succeeded) {
                throw new Exception($"AudioClip Load failed. key: {key}");
            }

            _loadedClips[key] = loadHandle;
            return loadHandle.Result;
        }

        private bool IsOverLimit(AudioGroupType group) {
            if (MaxConcurrent.TryGetValue(group, out var max) == false)
                return false;

            return _activeCountByGroup.TryGetValue(group, out var count) && count >= max;
        }

        private void IncrementActiveCount(AudioGroupType group) {
            _activeCountByGroup.TryGetValue(group, out var count);
            _activeCountByGroup[group] = count + 1;
        }

        private void DecrementActiveCount(AudioGroupType group) {
            if (_activeCountByGroup.TryGetValue(group, out var count) && count > 0) {
                _activeCountByGroup[group] = count - 1;
            }
        }

        private void ReturnPlayer(IAudioPlayer player) {
            ReturnById(player.GetInstance());

            // Track() 대상이 이미 파괴되어 player 자체가 죽어있으면 되돌릴 AudioSource/풀 슬롯이 없다 — 장부 정리만 한다.
            if (player.IsAlive == false)
                return;

            player.ReturnToIdle();
            _audioPooling.Push(player);
        }

        // 사이드 테이블(_activeGroups)만으로 카운트를 되돌린다 — Track() 대상이 파괴되어
        // player 자체가 Unity fake-null인 경우에도 안전하게 호출할 수 있다.
        private void ReturnById(int instanceId) {
            if (_activeGroups.TryGetValue(instanceId, out var group)) {
                DecrementActiveCount(group);
                _activeGroups.Remove(instanceId);
            }

            _activePlayers.Remove(instanceId);
        }

        private void PruneDestroyedPlayers() {
            _deadBuffer.Clear();

            foreach (var kv in _activePlayers) {
                if (kv.Value.IsAlive == false) {
                    _deadBuffer.Add(kv.Key);
                }
            }

            foreach (var id in _deadBuffer) {
                ReturnById(id);
            }
        }

        private async UniTask MonitorAsync(CancellationToken ct) {
            while (true) {
                ct.ThrowIfCancellationRequested();

                _finishedBuffer.Clear();
                foreach (var player in _activePlayers.Values) {
                    if (player.IsAlive && player.Loop == false && player.Source.isPlaying == false) {
                        _finishedBuffer.Add(player);
                    }
                }

                foreach (var player in _finishedBuffer) {
                    ReturnPlayer(player);
                }

                // Track() 대상이 재생 도중 Destroy되어 함께 사라진 AudioPlayer를 활성 목록에서 정리한다.
                PruneDestroyedPlayers();

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
    }
}