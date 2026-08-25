using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Script.Addressable;
using Script.GameInfo.Info;
using Script.GUI.ScreenData.Interface;
using Script.Tutorial;
using Script.Tutorial.Interface;
using SW.GUI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using FocusOption = Script.GUI.ScreenData.FocusOption;

namespace Script.GUI.Screen.Tutorial {
    public class TutorialFocusScreen : Screen, ITutorialFocus {
        private IAddressableService            _addressableService;
        private AddressableCacheHandle<Sprite> _leftIcon;
        private TutorialFocusData              _target;

        private float _baseAlpha   = 0.8f;
        private bool  _updateFocus = false;

        private DisposableBag _disposableBag = new();


        #region Reactive

        public ReactiveProperty<FocusGuide>     FocusInfo { get; private set; } = new();
        public ReadOnlyReactiveProperty<string> Name      { get; private set; }
        public ReadOnlyReactiveProperty<string> Text      { get; private set; }

        #endregion

        #region Inspector

        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform focus;
        [SerializeField] private RectTransform top;
        [SerializeField] private RectTransform bottom;
        [SerializeField] private RectTransform left;
        [SerializeField] private RectTransform right;

        [SerializeField] private RectTransform speechParent;
        [SerializeField] private TMP_Text      speechText;
        [SerializeField] private TMP_Text      speechNameText;

        [SerializeField] private Image leftIcon;

        [SerializeField] private Vector2 speechMargin;

        [SerializeField] private SW_GUI_BUTTON_BASE focusButton;

        [SerializeField] private List<Image>  rayCastImage;
        [SerializeField] private List<Image>  gardImages;

        #endregion

        public SW_GUI_BUTTON_BASE FocusButton => focusButton;

        [Inject]
        public void SelfInject(IAddressableService addressableService) {
            _addressableService =  addressableService;
        }
        
        protected override void AwakeInternal() {
            // 포커스 검정색 영역을 투명하게 해주기 위해서 처음 알파를 저장해둠
            _baseAlpha = gardImages.First().color.a;
        }

        public override async UniTask OpenInternal(IScreenOption screenOption, CancellationToken ct = default) {
            _disposableBag.Dispose();
            _disposableBag = new();

            Name = FocusInfo.Select(x => x?.name).ToReadOnlyReactiveProperty().AddTo(ref _disposableBag);
            Text = FocusInfo.Select(x => x?.guideText).ToReadOnlyReactiveProperty().AddTo(ref _disposableBag);
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

            if (screenOption is FocusOption focusOption) {
                await SetFocusAsync(focusOption.TutorialFocusData, focusOption.FocusGuide, ct);
            }
        }

        public override async UniTask OpenChangeOptionAsync(IScreenOption screenOption, CancellationToken ct = default) {
            if (screenOption is FocusOption focusOption) {
                await SetFocusAsync(focusOption.TutorialFocusData, focusOption.FocusGuide, ct);
            }
        }

        public override async UniTask CloseInternal() {
            await Release();
        }

        public override UniTask Release() {
            _disposableBag.Dispose();
            return UniTask.CompletedTask;
        }

        private void SetRayCast(bool isRayCast) {
            foreach (var image in rayCastImage) {
                image.raycastTarget = isRayCast;
            }
        }

        public void Stop(bool hide = true) {
            _updateFocus = false;
            _target      = null;

            if (hide) {
                Back();
            }
        }

        public async UniTask StopAsync(bool hide = true, CancellationToken ct = default) {
            _updateFocus = false;
            _target      = null;
            if (hide) {
                await CloseAsync(true, ct);
            }
        }

        private void SetFocus(TutorialFocusData focusData, FocusGuide focusGuide) {
            FocusInfo.OnNext(focusGuide);
            _target = focusData;
            var focusSize = ReSizeFocus();
            ResizeGard();
            SetSpeechPosition(focusGuide, focus.localPosition, focusSize);
            _updateFocus = true;
        }

        public async UniTask SetFocusAsync(TutorialFocusData focusData, FocusGuide focusGuide, CancellationToken ct = default) {
            if (focusData == null || focusGuide == null) {
                return;
            }

            await SetLeftImage(focusGuide, ct);
            SetFocus(focusData, focusGuide);
        }

        private void Update() {
            if (_updateFocus && FocusInfo?.CurrentValue != null) {
                var focusSize = ReSizeFocus();
                ResizeGard();
                SetSpeechPosition(FocusInfo.CurrentValue, focus.localPosition, focusSize);
            }
        }

        private async UniTask SetLeftImage(FocusGuide focusGuide, CancellationToken ct = default) {
            _leftIcon?.Dispose();
            _leftIcon       = await _addressableService.LoadAsync<Sprite>(focusGuide.iconPath, ct);
            leftIcon.sprite = _leftIcon.Value;
            var scale = leftIcon.transform.localScale;
            if (focusGuide.flip) {
                leftIcon.transform.localScale = new(Mathf.Abs(scale.x) * -1f, scale.y, scale.z);
            }
            else {
                leftIcon.transform.localScale = new(Mathf.Abs(scale.x), scale.y, scale.z);
            }
        }

        private void ResizeGard() {
            var canvasSize = root.rect;

            var screenWidth  = canvasSize.width;
            var screenHeight = canvasSize.height;

            var centerSize     = focus.sizeDelta;
            var centerPosition = focus.anchoredPosition;

            var topSizeY = screenHeight * 0.5f - (centerPosition.y + centerSize.y * 0.5f);
            top.sizeDelta        = new(0, topSizeY);
            top.anchoredPosition = new(0, topSizeY * -1f);

            var bottomSizeY = screenHeight * 0.5f + (centerPosition.y - centerSize.y * 0.5f);
            bottom.sizeDelta        = new(0, screenHeight * 0.5f + (centerPosition.y - centerSize.y * 0.5f));
            bottom.anchoredPosition = new(0, bottomSizeY * 1.0f);

            var leftSizeX = (screenWidth * 0.5f + centerPosition.x - centerSize.x * 0.5f);
            left.offsetMax        = new(leftSizeX, -topSizeY);
            left.offsetMin        = new(0, bottomSizeY);
            left.anchoredPosition = new(leftSizeX, centerPosition.y);

            var rightSizeX = (centerPosition.x + centerSize.x * 0.5f) - screenWidth * 0.5f;
            right.offsetMax        = new(0, -topSizeY);
            right.offsetMin        = new(rightSizeX, bottomSizeY);
            right.anchoredPosition = new(rightSizeX, centerPosition.y);
        }

        private Vector2 ReSizeFocus() {
            if (_target == null) return Vector2.zero;

            var sizeDelta   = _target.Size;
            var scaleFactor = new Vector2(_target.rtf.lossyScale.x, _target.rtf.lossyScale.y);
            var worldSize   = Vector2.Scale(sizeDelta, scaleFactor);

            focus.position = _target.Position;

            var pivotX = focus.pivot.x - _target.rtf.pivot.x;
            var pivotY = focus.pivot.y - _target.rtf.pivot.y;
            var pivotOffset = new Vector2(
                worldSize.x * (pivotX),
                worldSize.y * (pivotY)
            );

            focus.sizeDelta =  sizeDelta;
            focus.position  += (Vector3)pivotOffset;
            return sizeDelta;
        }

        private (Vector3 localPos, Vector2 sizeDelta) GetReSizeFocus() {
            if (_target == null) return (Vector2.zero, Vector2.zero);

            var worldPosition = _target.Position;

            var sizeDelta   = _target.Size;
            var scaleFactor = new Vector2(_target.rtf.lossyScale.x, _target.rtf.lossyScale.y);
            var worldSize   = Vector2.Scale(sizeDelta, scaleFactor);

            var localPosition = focus.parent.InverseTransformPoint(worldPosition);

            var pivotX = focus.pivot.x - _target.rtf.pivot.x;
            var pivotY = focus.pivot.y - _target.rtf.pivot.y;
            var pivotOffset = new Vector2(
                worldSize.x * (pivotX),
                worldSize.y * (pivotY)
            );

            return (localPosition + (Vector3)pivotOffset, sizeDelta);
        }

        private void SetSpeechPosition(FocusGuide focusGuide, Vector3 focusPosition, Vector2 focusSize) {
            if (speechParent == null || focusGuide == null) return;

            if (string.IsNullOrEmpty(focusGuide.guideText)) {
                speechParent.gameObject.SetActive(false);
                return;
            }

            var with = speechParent.rect.width;
            var height = speechParent.rect.height;

            var posY = focusPosition.y <= 0 ? (focusSize.y * 0.5f + focusPosition.y + height + speechMargin.y) : focusPosition.y - height - speechMargin.y - focusSize.y * 0.5f;

            var isLeft = focusPosition.x > 0;
            var posX   = 0f;
            if (isLeft) {
                posX -= speechMargin.x;
            }
            else {
                posX += speechMargin.x;
            }

            posX += focusPosition.x;

            var overFlowX = Mathf.Abs(focusPosition.x) + with * 0.5f - root.rect.width * 0.5f;
            if (overFlowX > 0) {
                posX = isLeft ? posX - overFlowX : posX + overFlowX;
            }

            speechParent.localPosition = new(posX, posY, 0);
        }

        public void SetGardAlpha(bool isOn) {
            var value = isOn ? _baseAlpha : 0f;
            foreach (var gardImage in gardImages) {
                gardImage.color = new Color(gardImage.color.r, gardImage.color.g, gardImage.color.b, value);
            }
        }

        public void SetButton(bool enable) {
            focusButton.enabled = enable;
            SetRayCast(enable);
        }
    }
}