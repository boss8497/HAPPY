using R3;
using Script.GameData.Model;
using Script.GamePlay.Audio.Interface;
using VContainer;

namespace Script.GUI.ViewModel {
    public class AudioOptionViewModel : ViewModel {
        private ReactiveProperty<IAudioManager> AudioManager { get; set; } = new();

        public ReadOnlyReactiveProperty<AudioSettingModel> AudioSetting        { get; set; }
        public ReadOnlyReactiveProperty<AudioSettingModel> CurrentAudioSetting { get; set; }
        public ReadOnlyReactiveProperty<bool>              IsChanged           { get; set; }

        private DisposableBag _disposableBag;

        [Inject]
        public void InjectSelf(
            IAudioManager audioManager
        ) {
            AudioManager.OnNext(audioManager);
        }

        public override void InitializeInternal() {
            autoInitializeState = false;
            InitializeReactiveProperty();
        }

        private void InitializeReactiveProperty() {
            AudioSetting = AudioManager.Select(i => i?.AudioSettingModel == null ? Observable.Empty<AudioSettingModel>() : Observable.Return(i.AudioSettingModel))
                                       .Switch()
                                       .ToReadOnlyReactiveProperty()
                                       .AddTo(ref _disposableBag);

            CurrentAudioSetting = AudioManager
                                  .Select(i => i?.AudioSettingModel == null ? Observable.Empty<AudioSettingModel>() : Observable.Return(i.AudioSettingModel.Clone() as AudioSettingModel))
                                  .Switch()
                                  .ToReadOnlyReactiveProperty()
                                  .AddTo(ref _disposableBag);


            IsChanged = AudioSetting.CombineLatest(CurrentAudioSetting, (a, b) => {
                                        if (a == null || b == null) return false;
                                        return !a.Equals(b);
                                    })
                                    .ToReadOnlyReactiveProperty()
                                    .AddTo(ref _disposableBag);


            AudioManager.Subscribe(audioManager => {
                            if (audioManager != null) {
                                AddState(ViewModelState.Initialized);
                            }
                        })
                        .AddTo(ref _disposableBag);
            AudioManager.ForceNotify();
        }

        public override void DisableInternal() { }

        private void DisableReactiveProperty() {
            AudioSetting        = null;
            CurrentAudioSetting = null;
            IsChanged           = null;
            _disposableBag.Dispose();
        }

        public override void DisposeInternal() {
            _disposableBag.Dispose();
        }
    }
}