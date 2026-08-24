using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Script.Addressable;
using Script.GameInfo.Info;
using Script.GUI.ScreenData.Interface;
using SW.GUI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.GUI.Screen.Tutorial {
    public class TutorialNarrationScreen : Screen {
        private IAddressableService            _addressableService;
        private AddressableCacheHandle<Sprite> _assetIcon;

        private DisposableBag _disposableBag = new();


        public ReactiveProperty<NarrationGuide> NarrationInfo { get; private set; } = new();
        public ReadOnlyReactiveProperty<string> Name          { get; private set; }
        public ReadOnlyReactiveProperty<string> Text          { get; private set; }


        [SerializeField] private SW_GUI_BUTTON_BASE nextButton;
        
        [SerializeField] private TMP_Text           speechText;
        [SerializeField] private TMP_Text           speechNameText;

        [SerializeField] private Image icon;

        public override UniTask OpenInternal(IScreenOption screenOption, CancellationToken ct = default) {
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

            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            throw new System.NotImplementedException();
        }
    }
}