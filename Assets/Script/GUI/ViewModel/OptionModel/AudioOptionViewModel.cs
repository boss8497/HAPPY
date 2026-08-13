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

        public ReactiveProperty<float> BgmVolume     { get; set; }
        public ReactiveProperty<bool>  BgmVolumeMute { get; set; }

        public ReactiveProperty<float> EffectVolume     { get; set; }
        public ReactiveProperty<bool>  EffectVolumeMute { get; set; }

        public ReactiveProperty<float> VoiceVolume     { get; set; }
        public ReactiveProperty<bool>  VoiceVolumeMute { get; set; }

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

            UpdateChanged = new();

            MasterVolume       = new();
            masterVolume.value = -1;
            MasterVolumeMute   = new();

            BgmVolume       = new();
            bgmVolume.value = -1;
            BgmVolumeMute   = new();

            EffectVolume       = new();
            effectVolume.value = -1;
            EffectVolumeMute   = new();

            VoiceVolume       = new();
            voiceVolume.value = -1;
            VoiceVolumeMute   = new();

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

                               bgmVolume.value = setting.bgmVolume;
                               bgmVolumeMute.SetIsOn(setting.bgmMute, true);

                               effectVolume.value = setting.effectVolume;
                               effectVolumeMute.SetIsOn(setting.effectMute, true);

                               voiceVolume.value = setting.voiceVolume;
                               voiceVolumeMute.SetIsOn(setting.voiceMute, true);

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

            MasterVolumeMute.CombineLatest(AudioSetting, MasterVolume, IsInitialized, (mute, setting, volume, init) => (mute, setting, volume, init))
                            .Subscribe((master) => {
                                if (master.setting == null || master.init == false || master.volume < 0) return;

                                if (master.setting.masterMute != master.mute) {
                                    AudioManager.Value.SetMute(AudioGroup.Master, master.mute, false);
                                    UpdateChanged.ForceNotify();
                                }
                            })
                            .AddTo(ref _disposableBag);


            BgmVolume.CombineLatest(AudioSetting, IsInitialized, (volume, setting, init) => (volume, setting, init))
                     .Subscribe((volume) => {
                         if (volume.setting == null || volume.init == false || volume.volume < 0) return;

                         if (Mathf.Abs(volume.setting.bgmVolume - volume.volume) > float.Epsilon) {
                             AudioManager.Value.SetVolume(AudioGroup.BGM, volume.volume, false);
                             UpdateChanged.ForceNotify();
                         }
                     })
                     .AddTo(ref _disposableBag);

            BgmVolumeMute.CombineLatest(AudioSetting, BgmVolume, IsInitialized, (mute, setting, volume, init) => (mute, setting, volume, init))
                         .Subscribe((bgm) => {
                             if (bgm.setting == null || bgm.init == false || bgm.volume < 0) return;

                             if (bgm.setting.bgmMute != bgm.mute) {
                                 AudioManager.Value.SetMute(AudioGroup.BGM, bgm.mute, false);
                                 UpdateChanged.ForceNotify();
                             }
                         })
                         .AddTo(ref _disposableBag);


            EffectVolume.CombineLatest(AudioSetting, IsInitialized, (volume, setting, init) => (volume, setting, init))
                        .Subscribe((effect) => {
                            if (effect.setting == null || effect.init == false || effect.volume < 0) return;

                            if (Mathf.Abs(effect.setting.effectVolume - effect.volume) > float.Epsilon) {
                                AudioManager.Value.SetVolume(AudioGroup.Effect, effect.volume, false);
                                UpdateChanged.ForceNotify();
                            }
                        })
                        .AddTo(ref _disposableBag);

            EffectVolumeMute.CombineLatest(AudioSetting, EffectVolume, IsInitialized, (mute, setting, volume, init) => (mute, setting, volume, init))
                            .Subscribe((effect) => {
                                if (effect.setting == null || effect.init == false || effect.volume < 0) return;

                                if (effect.setting.effectMute != effect.mute) {
                                    AudioManager.Value.SetMute(AudioGroup.Effect, effect.mute, false);
                                    UpdateChanged.ForceNotify();
                                }
                            })
                            .AddTo(ref _disposableBag);


            VoiceVolume.CombineLatest(AudioSetting, IsInitialized, (volume, setting, init) => (volume, setting, init))
                       .Subscribe((voice) => {
                           if (voice.setting == null || voice.init == false || voice.volume < 0) return;

                           if (Mathf.Abs(voice.setting.voiceVolume - voice.volume) > float.Epsilon) {
                               AudioManager.Value.SetVolume(AudioGroup.Voice, voice.volume, false);
                               UpdateChanged.ForceNotify();
                           }
                       })
                       .AddTo(ref _disposableBag);

            VoiceVolumeMute.CombineLatest(AudioSetting, VoiceVolume, IsInitialized, (mute, setting, volume, init) => (mute, setting, volume, init))
                           .Subscribe((voice) => {
                               if (voice.setting == null || voice.init == false || voice.volume < 0) return;

                               if (voice.setting.voiceMute != voice.mute) {
                                   AudioManager.Value.SetMute(AudioGroup.Voice, voice.mute, false);
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

            bgmVolume.onValueChanged.RemoveAllListeners();
            bgmVolume.onValueChanged.AddListener((value) => { BgmVolume.OnNext(value); });

            effectVolume.onValueChanged.RemoveAllListeners();
            effectVolume.onValueChanged.AddListener((value) => { EffectVolume.OnNext(value); });

            voiceVolume.onValueChanged.RemoveAllListeners();
            voiceVolume.onValueChanged.AddListener((value) => { VoiceVolume.OnNext(value); });

            masterVolumeMute.AddValueChangedListener((value) => { MasterVolumeMute.OnNext(value); });
            bgmVolumeMute.AddValueChangedListener((value) => { BgmVolumeMute.OnNext(value); });
            effectVolumeMute.AddValueChangedListener((value) => { EffectVolumeMute.OnNext(value); });
            voiceVolumeMute.AddValueChangedListener((value) => { VoiceVolumeMute.OnNext(value); });
        }

        public override void DisableInternal() {
            DisableReactiveProperty();
        }

        private void DisableReactiveProperty() {
            _disposableBag.Dispose();

            masterVolume.onValueChanged.RemoveAllListeners();
            bgmVolume.onValueChanged.RemoveAllListeners();
            effectVolume.onValueChanged.RemoveAllListeners();
            voiceVolume.onValueChanged.RemoveAllListeners();

            BackUpAudioSetting = null;
            AudioSetting       = null;
            UpdateChanged      = null;

            MasterVolume     = null;
            MasterVolumeMute = null;

            BgmVolume     = null;
            BgmVolumeMute = null;

            EffectVolume     = null;
            EffectVolumeMute = null;

            VoiceVolume     = null;
            VoiceVolumeMute = null;
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