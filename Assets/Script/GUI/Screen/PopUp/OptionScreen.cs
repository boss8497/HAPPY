using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Script.GameData.Model;
using Script.GamePlay.Audio.Interface;
using Script.GUI.ScreenData;
using Script.GUI.ScreenData.Interface;
using Script.GUI.ViewModel;
using SW.GUI;
using VContainer;

namespace Script.GUI.Screen {
    public class OptionScreen : Screen {
        #region Inspector

        public AudioOptionViewModel audioOptionViewModel;

        public SW_GUI_BUTTON_SIMPLE backBtn;
        public SW_GUI_BUTTON        changedButton;

        #endregion

        private IAudioManager _audioManager;

        private ReadOnlyReactiveProperty<bool> IsAudioChanged { get; set; }


        private CancellationTokenSource _changedCts;
        private DisposableBag           _disposableBag;

        [Inject]
        public void InjectSelf(
            IAudioManager audioManager
        ) {
            _audioManager = audioManager;
        }

        public override UniTask OpenInternal(IScreenOption screenOption) {
            InitializeReactiveProperty();

            backBtn.AddClickListener(() => {
                if (_changedCts != null) {
                    return;
                }

                if (IsAudioChanged?.CurrentValue ?? false) {
                    _changedCts = new();
                    ApplyAudioOption(_changedCts.Token).Forget();
                }
                else {
                    Back();
                }
            });

            changedButton.AddClickListener(() => {
                if (_changedCts != null) {
                    return;
                }

                _changedCts = new();
                ApplyAudioOption(_changedCts.Token).Forget();
            });

            return UniTask.CompletedTask;
        }

        private async UniTask ApplyAudioOption(CancellationToken ct) {
            var end     = false;
            var changed = false;
            await ScreenManager.OpenMessage("변경", "변경하시겠습니까?", null, MessageType.OkCancel,
                                            () => {
                                                changed = true;
                                                end     = true;
                                            },
                                            () => {
                                                changed = false;
                                                end     = true;
                                            }, ct: ct);


            var cancel = await UniTask.WaitUntil(() => end, cancellationToken: ct).SuppressCancellationThrow();
            if (cancel) {
                return;
            }

            if (changed) {
                await audioOptionViewModel.ChangeAudioSetting(audioOptionViewModel.AudioSetting?.CurrentValue, ct);
            }
            else {
                await audioOptionViewModel.ChangeAudioSetting(audioOptionViewModel.BackUpAudioSetting?.CurrentValue, ct);
            }

            if (_changedCts != null) {
                _changedCts.Cancel();
                _changedCts = null;
            }

            await BackAsync();
        }

        private void InitializeReactiveProperty() {
            _disposableBag.Dispose();
            _disposableBag = new();

            IsAudioChanged = audioOptionViewModel.IsChanged.Select(i => i)
                                                 .ToReadOnlyReactiveProperty()
                                                 .AddTo(ref _disposableBag);

            IsAudioChanged.Subscribe(isChanged => { changedButton.Interactable = isChanged; }).AddTo(ref _disposableBag);
        }

        public override UniTask CloseInternal() {
            if (_changedCts != null) {
                _changedCts.Cancel();
                _changedCts = null;
            }

            _disposableBag.Dispose();
            return UniTask.CompletedTask;
        }
    }
}