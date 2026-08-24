using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Script.Addressable;
using Script.GameInfo.Info;
using Script.GUI.ScreenData;
using Script.GUI.ScreenData.Interface;
using Script.Tutorial.Interface;
using SW.GUI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen.Tutorial {
    public class TutorialNarrationScreen : Screen, ITutorialScreen {
        private IAddressableService            _addressableService;
        private AddressableCacheHandle<Sprite> _assetIcon;
        private Action                         _completeCallBack;

        private DisposableBag _disposableBag = new();


        public ReactiveProperty<NarrationGuide> NarrationInfo { get; private set; } = new();
        public ReadOnlyReactiveProperty<string> Name          { get; private set; }
        public ReadOnlyReactiveProperty<string> Text          { get; private set; }


        [SerializeField] private SW_GUI_BUTTON_BASE nextButton;

        [SerializeField] private TMP_Text speechText;
        [SerializeField] private TMP_Text speechNameText;

        [SerializeField] private Image icon;


        [Inject]
        public void SelfInject(IAddressableService addressableService) {
            _addressableService = addressableService;
        }

        protected override void AwakeInternal() {
            nextButton.AddClickAsyncListener(() => {
                _completeCallBack?.Invoke();
                return UniTask.CompletedTask;
            }, false);
            base.AwakeInternal();
        }

        public override async UniTask OpenInternal(IScreenOption screenOption, CancellationToken ct = default) {
            _disposableBag.Dispose();
            _disposableBag = new();
            

            Name = NarrationInfo.Select(x => x?.name).ToReadOnlyReactiveProperty().AddTo(ref _disposableBag);
            Text = NarrationInfo.Select(x => x?.guideText).ToReadOnlyReactiveProperty().AddTo(ref _disposableBag);
            Name.Subscribe(speechName => {
                    if (speechNameText != null) {
                        speechNameText.SetText(speechName);
                    }
                })
                .AddTo(ref _disposableBag);

            Text.Subscribe(text => {
                    if (speechText != null) {
                        speechText.SetText(text);
                    }
                })
                .AddTo(ref _disposableBag);

            if (screenOption is NarrationOption narrationOption) {
                _completeCallBack = narrationOption.CompleteCallBack;
                await SetNarrationAsync(narrationOption, ct);
            }
        }

        public override async UniTask OpenChangeOptionAsync(IScreenOption screenOption, CancellationToken ct = default) {
            if (screenOption is NarrationOption narrationOption) {
                await SetNarrationAsync(narrationOption, ct);
            }
        }

        private async UniTask SetNarrationAsync(NarrationOption screenOption, CancellationToken ct = default) {
            NarrationInfo.OnNext(screenOption.NarrationGuide);
            await SetImage(screenOption.NarrationGuide, ct);
        }

        public override UniTask CloseInternal() {
            Release();
            return UniTask.CompletedTask;
        }

        public override UniTask Release() {
            _completeCallBack = null;
            _disposableBag.Dispose();
            return UniTask.CompletedTask;
        }


        private async UniTask SetImage(NarrationGuide narrationGuide, CancellationToken ct = default) {
            _assetIcon?.Dispose();
            _assetIcon  = await _addressableService.LoadAsync<Sprite>(narrationGuide.iconPath, ct);
            icon.sprite = _assetIcon.Value;
            var scale = icon.transform.localScale;

            if (narrationGuide.flip) {
                icon.transform.localScale = new(Mathf.Abs(scale.x) * -1f, scale.y, scale.z);
            }
            else {
                icon.transform.localScale = new(Mathf.Abs(scale.x), scale.y, scale.z);
            }
        }

        public async UniTask StopAsync(bool hide = true, CancellationToken ct = default) {
            if (hide) {
                await CloseAsync(true, ct);
            }
        }
    }
}