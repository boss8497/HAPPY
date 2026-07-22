using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
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
        private readonly Dictionary<int, IAudioPlayer> _activePlayers      = new();
        private readonly Dictionary<int, AudioGroup>   _activeGroups       = new();
        private readonly Dictionary<AudioGroup, int>   _activeCountByGroup = new();

        // 그룹별 MaxCount 초과 시 가장 오래 재생 중인 플레이어를 골라내기 위한 대여 순번(작을수록 오래됨).
        private readonly Dictionary<int, int> _rentOrder = new();
        private          int                  _rentSequence;

        private readonly List<IAudioPlayer> _finishedBuffer = new();
        private readonly List<int>          _deadBuffer     = new();

        public async UniTask<AudioHandle> PlayAsync(
            string            key,
            AudioGroup        group       = AudioGroup.Effect,
            bool              loop        = false,
            bool              autoRelease = true,
            float             pitch       = 1f,
            bool              is3D        = false,
            Vector3?          position    = null,
            Transform         track       = null,
            CancellationToken ct          = default
        ) {
            // 같은 key가 이미 재생 중이면 새로 빌리지 않고 그 인스턴스를 재사용해 처음부터 다시 재생한다.
            // 새 슬롯을 점유하지 않으므로 IsOverLimit로 막혀 있는 상황이어도 이 재생 요청은 통과시킨다.
            var existing = FindActiveByClipKey(key);
            if (existing != null) {
                return Replay(existing, group, loop, pitch, is3D, position, track);
            }

            var clip = await GetOrLoadClipAsync(key, ct);

            // MaxCount 체크는 실제로 풀 슬롯을 점유하기 직전(로드 완료 후)에 한다.
            // 상한을 넘었으면 거부하는 대신 그룹에서 가장 오래 재생 중인 플레이어를 강제 종료(Dequeue)하고 자리를 내준다.
            // BGM은 이 풀 자체를 거치지 않으므로(PlayBGM 참고) 대상이 될 수 없다.
            if (IsOverLimit(group)) {
                var oldest = FindOldestInGroup(group);
                if (oldest == null) {
                    return AudioHandle.Invalid;
                }

                ReturnPlayer(oldest);
            }

            var player = _audioPooling.Pop();
            var handle = player.Rent(group, key, loop);

            ApplyPlaybackSettings(player, clip, group, loop, pitch, is3D, position, track);
            player.Source.Play();

            RegisterActive(handle, player, group);

            // 요청대로 단발 재생(loop=false) + autoRelease면 재생 시작 직후 Addressable 핸들만 Release한다.
            // AudioSource는 이미 clip을 참조 중이라 재생 자체는 계속 이어진다(압축 스트리밍 클립은 예외적 위험 있음, ARCHITECTURE.md 참고).
            // loop=true(BGM 등)는 재생 중 캐시를 지우면 안 되므로 autoRelease를 무시한다.
            if (loop == false && autoRelease) {
                ReleaseClipHandle(key);
            }

            return handle;
        }

        public async UniTask<AudioHandle> PlayAsync(
            AudioData         audioData,
            Vector3?          position = null,
            Transform         track    = null,
            CancellationToken ct       = default
        ) {
            if (audioData == null)
                return AudioHandle.Invalid;

            // 같은 key가 이미 재생 중이면 새로 빌리지 않고 그 인스턴스를 재사용해 처음부터 다시 재생한다.
            // 새 슬롯을 점유하지 않으므로 IsOverLimit로 막혀 있는 상황이어도 이 재생 요청은 통과시킨다.
            var existing = FindActiveByClipKey(audioData.key);
            if (existing != null) {
                return Replay(existing, audioData.type, audioData.loop, audioData.pitch, audioData.is3D, position, track);
            }

            var clip = await GetOrLoadClipAsync(audioData.key, ct);

            // MaxCount 체크는 실제로 풀 슬롯을 점유하기 직전(로드 완료 후)에 한다.
            // 상한을 넘었으면 거부하는 대신 그룹에서 가장 오래 재생 중인 플레이어를 강제 종료(Dequeue)하고 자리를 내준다.
            // BGM은 이 풀 자체를 거치지 않으므로(PlayBGM 참고) 대상이 될 수 없다.
            if (IsOverLimit(audioData.type)) {
                var oldest = FindOldestInGroup(audioData.type);
                if (oldest == null) {
                    return AudioHandle.Invalid;
                }

                ReturnPlayer(oldest);
            }

            var player = _audioPooling.Pop();
            var handle = player.Rent(audioData.type, audioData.key, audioData.loop);

            ApplyPlaybackSettings(player, clip, audioData.type, audioData.loop, audioData.pitch, audioData.is3D, position, track);
            player.Source.Play();

            RegisterActive(handle, player, audioData.type);

            // 요청대로 단발 재생(loop=false) + autoRelease면 재생 시작 직후 Addressable 핸들만 Release한다.
            // AudioSource는 이미 clip을 참조 중이라 재생 자체는 계속 이어진다(압축 스트리밍 클립은 예외적 위험 있음, ARCHITECTURE.md 참고).
            // loop=true(BGM 등)는 재생 중 캐시를 지우면 안 되므로 autoRelease를 무시한다.
            if (audioData.loop == false && audioData.autoRelease) {
                ReleaseClipHandle(audioData.key);
            }

            return handle;
        }

        private AudioHandle Replay(
            IAudioPlayer player,
            AudioGroup   group,
            bool         loop,
            float        pitch,
            bool         is3D,
            Vector3?     position,
            Transform    track
        ) {
            var previousGroup = player.Group;
            var handle        = player.Rent(group, player.ClipKey, loop);

            ApplyPlaybackSettings(player, player.Source.clip, group, loop, pitch, is3D, position, track);
            player.Source.time = 0f;
            player.Source.Play();

            if (previousGroup != group) {
                DecrementActiveCount(previousGroup);
                IncrementActiveCount(group);
                _activeGroups[handle.InstanceId] = group;
            }

            // 방금 다시 재생을 시작했으니 대여 순번을 갱신해 곧바로 Dequeue 대상이 되지 않게 한다.
            _rentOrder[handle.InstanceId] = ++_rentSequence;

            return handle;
        }

        private void ApplyPlaybackSettings(
            IAudioPlayer player,
            AudioClip    clip,
            AudioGroup   group,
            bool         loop,
            float        pitch,
            bool         is3D,
            Vector3?     position,
            Transform    track
        ) {
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
        }

        private void RegisterActive(AudioHandle handle, IAudioPlayer player, AudioGroup group) {
            _activePlayers[handle.InstanceId] = player;
            _activeGroups[handle.InstanceId]  = group;
            _rentOrder[handle.InstanceId]     = ++_rentSequence;
            IncrementActiveCount(group);
        }

        private IAudioPlayer FindActiveByClipKey(string key) {
            foreach (var player in _activePlayers.Values) {
                if (player.IsAlive && player.ClipKey == key) {
                    return player;
                }
            }

            return null;
        }

        private IAudioPlayer FindOldestInGroup(AudioGroup group) {
            IAudioPlayer oldest      = null;
            var          oldestOrder = int.MaxValue;

            foreach (var kv in _activePlayers) {
                var player = kv.Value;
                if (player.IsAlive == false || player.Group != group)
                    continue;

                if (_rentOrder.TryGetValue(kv.Key, out var order) && order < oldestOrder) {
                    oldestOrder = order;
                    oldest      = player;
                }
            }

            return oldest;
        }

        public async UniTask PlayBGM(string key, CancellationToken ct = default) {
            if (key == null) {
                StopBGM();
                return;
            }
            
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

        public void Stop(string key) {
            StopAllByClip(key);
        }

        public void Stop(AudioData audioData) {
            if (audioData == null)
                return;

            StopAllByClip(audioData.key);
        }

        public void StopAll(AudioGroup group) {
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

        private bool IsOverLimit(AudioGroup group) {
            if (MaxConcurrent.TryGetValue(group, out var max) == false)
                return false;

            return _activeCountByGroup.TryGetValue(group, out var count) && count >= max;
        }

        private void IncrementActiveCount(AudioGroup group) {
            _activeCountByGroup.TryGetValue(group, out var count);
            _activeCountByGroup[group] = count + 1;
        }

        private void DecrementActiveCount(AudioGroup group) {
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

            _rentOrder.Remove(instanceId);
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