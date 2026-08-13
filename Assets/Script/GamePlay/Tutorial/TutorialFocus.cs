using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using Script.GameInfo.Attribute;
using Script.GameInfo.Info;
using Script.GamePlay.Service.Interface;
using Script.Tutorial.Interface;
using Sirenix.OdinInspector;
using SW.GUI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.Tutorial {
    public class TutorialFocus : MonoBehaviour, ITutorialFocus {
        private ITutorialService _tutorialService;

        #region Reactive

        public ReactiveProperty<FocusGuide>   FocusInfo    { get; private set; } = new();
        public ReadOnlyReactiveProperty<bool> IsScreenShow { get; private set; }

        public ReadOnlyReactiveProperty<string> Name     { get; private set; }
        public ReadOnlyReactiveProperty<string> NickName { get; private set; }

        #endregion

        #region Inspector

        public bool          updateFocusGard = false;
        public RectTransform root;
        public RectTransform focus;
        public RectTransform top;
        public RectTransform bottom;
        public RectTransform left;
        public RectTransform right;

        public RectTransform speechParent;
        public TMP_Text      speechText;

        public SpeechObject LeftSpeechObjectData;
        public SpeechObject RightSpeechObjectData;

        public Vector2 speechMargin;

        public SW_GUI_BUTTON_BASE focusButton;
        public SW_GUI_BUTTON_BASE FocusButton => focusButton;

        public List<Image> rayCastImage;
        public List<Image> gardImages;

        #endregion

        private TutorialFocusData _target;

        private float _baseAlpha = 0.8f;
        
        private bool  _onFocus   = false;
        public  bool  OnFocus => _onFocus;


        public TutorialFocusData testObject;

        [Focus]
        public Guid testGuideGuid;
        private DisposableBag _disposableBag = new();

        [Button("TestStartFocus")]
        public void TestStartFocus() {
            SetFocusAsync(testObject, null).Forget();
        }

        [Button("TestStopFocus")]
        public void TestStopFocus() {
            StopAsync().Forget();
        }

        
        [Inject]
        public void InjectSelf(
            ITutorialService  tutorialService
        ) {
            _tutorialService = tutorialService;
        }

        public UniTask OnInitialize() {
            Initialize();

            SetScreen();

            // IsScreenShow = screen?.IsShow.Select(i => i)
            //                      .DistinctUntilChanged()
            //                      .ToReadOnlyReactiveProperty()
            //                      .AddTo(ref _disposableBag);
            //
            // if (screen != null) screen.gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        public void ReleaseAddressable() {
            LeftSpeechObjectData?.Dispose();
            RightSpeechObjectData?.Dispose();
        }

        public void OnFinalize() {
            ReleaseAddressable();
            _disposableBag.Dispose();
        }

        private void SetScreen() {
            // if (screen == null) {
            //     screen = GetComponent<Screen>();
            // }
            //
            // if (screen == null) {
            //     screen = GetComponentInParent<Screen>();
            // }
            //
            // if (screen == null) {
            //     screen = GetComponentInChildren<Screen>();
            // }
        }

        private void Initialize() {
            if (root == null) {
                root = transform.GetComponentInParent<Canvas>()?.transform as RectTransform;
            }

            _tutorialService?.RegisterFocus(this);
            Name     = FocusInfo.Select(x => x?.name).ToReadOnlyReactiveProperty().AddTo(ref _disposableBag);

            _baseAlpha = gardImages.First().color.a;
        }

        public void SetRayCast(bool isRayCast) {
            foreach (var image in rayCastImage) {
                image.raycastTarget = isRayCast;
            }
        }

        public bool IsShow() {
            //return screen.IsShow.CurrentValue;
            return false;
        }

        public void Stop(bool hide = true) {
            updateFocusGard = false;
            _onFocus        = false;
            _target         = null;

            // if (hide && screen != null) {
            //     screen.Hide();
            //     FocusInfo.Value = null;
            // }
        }

        public UniTask ScreenHide() {
            // await screen.HideAsync();
            // screen.gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        public UniTask StopAsync(bool hide = true) {
            updateFocusGard = false;
            _onFocus        = false;
            _target         = null;

            // if (hide && screen != null) {
            //     await screen.HideAsync();
            //     screen.gameObject.SetActive(false);
            // }

            LeftSpeechObjectData?.Dispose();
            RightSpeechObjectData?.Dispose();
            return UniTask.CompletedTask;
        }

        public void SetFocus(TutorialFocusData focusData, FocusGuide focusGuide) {
            // screen?.gameObject.SetActive(true);
            // screen?.Show();
            _target = focusData;
            var focusSize = ReSizeFocus();
            ResizeGard();
            SetSpeechPosition(focusGuide, focus.localPosition, focusSize);
            _onFocus = true;
        }

        public UniTask SetFocusAnimation(TutorialFocusData focusData, FocusGuide focusGuide) {
            FocusInfo.Value = focusGuide;
            var (movePos, sizeDelta)                  = GetReSizeFocus();
            
            SetSpeechPosition(focusGuide, movePos, sizeDelta);
            _onFocus        = true;
            updateFocusGard = true;
            return UniTask.CompletedTask;
        }

        public UniTask SetFocusAsync(TutorialFocusData focusData, FocusGuide focusGuide) {
            FocusInfo.Value = focusGuide;
            // screen.gameObject.SetActive(true);
            // await screen.ShowAsync();
            _target = focusData;
            var focusSize = ReSizeFocus();
            ResizeGard();
            SetSpeechPosition(focusGuide, focus.localPosition, focusSize);
            _onFocus = true;
            FocusUpdate(focusData, focusGuide).Forget();
            return UniTask.CompletedTask;
        }

        private async UniTask FocusUpdate(TutorialFocusData focusData, FocusGuide focusGuide) {
            while (_onFocus) {
                _target = focusData;
                var focusSize = ReSizeFocus();
                ResizeGard();
                SetSpeechPosition(focusGuide, focus.localPosition, focusSize);
                await UniTask.WaitForFixedUpdate();
            }
        }

        private void ResizeGard() {
            var canvasSize = root.sizeDelta;

            var screenWidth  = canvasSize.x;
            var screenHeight = canvasSize.y;

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

            speechText.text = focusGuide.guideText;
            speechParent.gameObject.SetActive(true);

            var posY = focusPosition.y <= 0 ? (focusSize.y * 0.5f + focusPosition.y + speechParent.sizeDelta.y + speechMargin.y) : focusPosition.y - speechParent.sizeDelta.y - speechMargin.y - focusSize.y * 0.5f;

            var isLeft = focusPosition.x > 0;
            var posX   = 0f;
            if (isLeft) {
                posX -= speechMargin.x;
            }
            else {
                posX += speechMargin.x;
            }

            LeftSpeechObjectData.On(focusGuide.iconPath, focusGuide.flip);

            posX += focusPosition.x;

            var overFlowX = Mathf.Abs(focusPosition.x) + speechParent.sizeDelta.x * 0.5f - root.sizeDelta.x * 0.5f;
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

        private void Update() {
            if (updateFocusGard) {
                ResizeGard();
            }
        }
    }
}