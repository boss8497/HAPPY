using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.DataBase.Interface;
using Script.GamePlay.Audio.Interface;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

namespace Script.GamePlay.Audio {
    /// <summary>
    /// Master/BGM/Effect/Voice AudioMixerGroup을 기반으로 오디오를 재생/관리한다.
    /// Addressable(key="AudioMixer")과 DataBase가 준비된 이후 진행되어야 하므로
    /// IInitializable.Initialize() 자동 실행 대신 GameSetting과 동일하게 StartUpLogic이
    /// InitializeAudioManager()를 명시적으로 호출하는 2단계 부팅 패턴을 따른다.
    /// </summary>
    public partial class AudioManager : IAudioManager, IInitializable, IDisposable {
        private const string MixerKey = "AudioMixer";

        // 그룹별 동시 재생 제한. BGM은 별도 전용 AudioSource로 관리되므로 여기 포함하지 않는다.
        private static readonly Dictionary<AudioGroupType, int> MaxConcurrent = new() {
            { AudioGroupType.Master, 4 },
            { AudioGroupType.Effect, 8 },
            { AudioGroupType.Voice, 3 },
        };

        private readonly IDataBase     _dataBase;
        private readonly AudioSetting  _audioSetting;
        private readonly IAudioPooling _audioPooling;

        private          AsyncOperationHandle<AudioMixer>            _mixerHandle;
        private          AudioMixer                                  _mixer;
        private readonly Dictionary<AudioGroupType, AudioMixerGroup> _mixerGroups = new();

        private AudioSource _bgmSource;

        private CancellationTokenSource _cts;

        public bool Initialized { get; private set; }

        public AudioManager(IDataBase dataBase, IAudioPooling audioPooling) {
            _dataBase     = dataBase ?? throw new ArgumentNullException(nameof(dataBase));
            _audioPooling = audioPooling ?? throw new ArgumentNullException(nameof(audioPooling));
            _audioSetting = new AudioSetting(dataBase);
        }

        public void Initialize() {
            // 실제 초기화는 InitializeAudioManager()에서 명시적으로 시작한다.
        }

        public void InitializeAudioManager() {
            _cts = new CancellationTokenSource();
            InitializeAsync(_cts.Token).Forget();
        }

        private async UniTask InitializeAsync(CancellationToken ct) {
            await LoadMixerAsync(ct);
            CacheMixerGroups();
            CreateBgmSource();

            await UniTask.WaitUntil(() => _dataBase.Initialized, cancellationToken: ct);
            await _audioSetting.LoadAsync();

            ApplyVolume(AudioGroupType.Master);
            ApplyVolume(AudioGroupType.BGM);
            ApplyVolume(AudioGroupType.Effect);
            ApplyVolume(AudioGroupType.Voice);

            MonitorAsync(ct).Forget();

            Initialized = true;
        }

        private async UniTask LoadMixerAsync(CancellationToken ct) {
            _mixerHandle = Addressables.LoadAssetAsync<AudioMixer>(MixerKey);

            while (!_mixerHandle.IsDone) {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (_mixerHandle.Status != AsyncOperationStatus.Succeeded) {
                throw new Exception($"AudioMixer Load failed. key: {MixerKey}");
            }

            // AudioMixerGroup은 AudioMixer 서브 에셋을 그대로 참조하므로(Instantiate 복사본이 아님)
            // ScreenManager._loadedScreens처럼 앱 생존 기간 동안 핸들을 유지한 채로 사용한다.
            _mixer = _mixerHandle.Result;
        }

        private void CacheMixerGroups() {
            var groups = _mixer.FindMatchingGroups(string.Empty);
            foreach (var group in groups) {
                if (Enum.TryParse<AudioGroupType>(group.name, out var type)) {
                    _mixerGroups[type] = group;
                }
            }
        }

        private void CreateBgmSource() {
            var obj = new GameObject("BGMSource");
            UnityEngine.Object.DontDestroyOnLoad(obj);

            _bgmSource              = obj.AddComponent<AudioSource>();
            _bgmSource.playOnAwake  = false;
            _bgmSource.loop         = true;
            _bgmSource.volume       = 1f;
            _bgmSource.spatialBlend = 0f;

            if (_mixerGroups.TryGetValue(AudioGroupType.BGM, out var bgmGroup)) {
                _bgmSource.outputAudioMixerGroup = bgmGroup;
            }
        }

        public void Dispose() {
            if (_cts is { IsCancellationRequested: false }) {
                _cts.Cancel();
            }

            _cts?.Dispose();

            if (_mixerHandle.IsValid()) {
                Addressables.Release(_mixerHandle);
            }
        }
    }
}
