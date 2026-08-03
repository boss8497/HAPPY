using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Script.GameData.Model;
using Script.GameInfo.Info;
using Script.GamePlay.Audio.Interface;
using SW.GUI;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.ViewModel {
    public class AudioOptionViewModel : ViewModel {
        public Slider        masterVolume;
        public SW_GUI_TOGGLE masterVolumeMute;

        public Slider        bgmVolume;
        public SW_GUI_TOGGLE bgmVolumeMute;

        public Slider        effectVolume;
        public SW_GUI_TOGGLE effectVolumeMute;

        public Slider        voiceVolume;
        public SW_GUI_TOGGLE voiceVolumeMute;

        private ReactiveProperty<IAudioManager> AudioManager { get; set; } = new();

        public ReadOnlyReactiveProperty<AudioSettingModel> BackUpAudioSetting { get; set; }
        public ReadOnlyReactiveProperty<AudioSettingModel> AudioSetting       { get; set; }


        public ReactiveProperty<bool> IsChanged     { get; set; } = new();
        public ReactiveProperty<bool> UpdateChanged { get; set; }

        public ReactiveProperty<float> MasterVolume     { get; set; }
        public ReactiveProperty<bool>  MasterVolumeMute { get; set; }

        private DisposableBag _disposableBag;

        [Inject]
        public void InjectSelf(
            IAudioManager audioManager
        ) {
            AudioManager.OnNext(audioManager);
        }

        public override void InitializeInternal() {
            autoInitializeState = false;

            EventSetting();
            InitializeReactiveProperty();
        }

        private void InitializeReactiveProperty() {
            _disposableBag.Dispose();
            _disposableBag = new();
            
            UpdateChanged  = new();

            MasterVolume     = new(-1);
            MasterVolumeMute = new();

            BackUpAudioSetting = AudioManager.Select(i => i?.AudioSettingModel == null ? Observable.Empty<AudioSettingModel>() : Observable.Return(i.AudioSettingModel.Clone() as AudioSettingModel))
                                             .Switch()
                                             .ToReadOnlyReactiveProperty()
                                             .AddTo(ref _disposableBag);

            AudioSetting = AudioManager
                           .Select(i => {
                               if (i?.AudioSettingModel == null) {
                                   return Observable.Return<AudioSettingModel>(null);
                               }

                               var setting = i.AudioSettingModel;
                               if (setting == null) {
                                   return Observable.Return<AudioSettingModel>(null);
                               }

                               masterVolume.value = setting.masterVolume;
                               masterVolumeMute.SetIsOn(setting.masterMute, true);


                               return Observable.Return(setting);
                           })
                           .Switch()
                           .ToReadOnlyReactiveProperty()
                           .AddTo(ref _disposableBag);

            UpdateChanged.CombineLatest(BackUpAudioSetting, AudioSetting, (u, a, b) => {
                             if (a == null || b == null) return false;
                             return !a.Equals(b);
                         })
                         .Subscribe(changed => { IsChanged.OnNext(changed); })
                         .AddTo(ref _disposableBag);


            MasterVolume.CombineLatest(AudioSetting, IsInitialized, (volume, setting, init) => (volume, setting, init))
                        .Subscribe((volume) => {
                            if (volume.setting == null || volume.init == false || volume.volume < 0) return;

                            if (Mathf.Abs(volume.setting.masterVolume - volume.volume) > float.Epsilon) {
                                AudioManager.Value.SetVolume(AudioGroup.Master, volume.volume, false);
                                UpdateChanged.ForceNotify();
                            }
                        })
                        .AddTo(ref _disposableBag);

            MasterVolumeMute.CombineLatest(AudioSetting, (mute, setting) => (mute, setting))
                            .Subscribe((volume) => {
                                if (volume.setting == null) return;

                                if (volume.setting.masterMute != volume.mute) {
                                    AudioManager.Value.SetMute(AudioGroup.Master, volume.mute, false);
                                    UpdateChanged.ForceNotify();
                                }
                            })
                            .AddTo(ref _disposableBag);


            AudioManager.Subscribe(audioManager => {
                            if (audioManager != null) {
                                AddState(ViewModelState.Initialized);
                            }
                        })
                        .AddTo(ref _disposableBag);
            AudioManager.ForceNotify();
            UpdateChanged.ForceNotify();
        }

        private void EventSetting() {
            masterVolume.onValueChanged.RemoveAllListeners();
            masterVolume.onValueChanged.AddListener((value) => { MasterVolume.OnNext(value); });
        }

        public override void DisableInternal() {
            DisableReactiveProperty();
        }

        private void DisableReactiveProperty() {
            _disposableBag.Dispose();

            masterVolume.onValueChanged.RemoveAllListeners();

            BackUpAudioSetting = null;
            AudioSetting       = null;
            UpdateChanged      = null;

            MasterVolume     = null;
            MasterVolumeMute = null;
        }

        public override void DisposeInternal() {
            _disposableBag.Dispose();
        }

        public async UniTask ChangeAudioSetting(AudioSettingModel setting, CancellationToken ct = default) {
            if (setting == null) return;
            await AudioManager.CurrentValue.ChangeAudioSetting(setting, ct);
        }
    }
}