using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.GamePlay.Service.Interface;
using Script.GUI.Screen.Interface;
using Script.GUI.ScreenData;
using Script.Tutorial;
using Script.Tutorial.Interface;
using SW.GUI.Base;
using VContainer.Unity;

namespace Script.GamePlay.Service {
    public class FocusService : IFocusService, IInitializable, IDisposable {
        private const string FocusScreenKey = "TutorialFocus";

        public bool Initialized { get; private set; }

        private Dictionary<string, TutorialFocusData> _focusDic = new();

        //서버에게 Req를 보내고 Ack를 받는 상황의 Ui에서 막아주는게 필요
        public bool BlockButton { get; set; } = false;


        private          ITutorialFocus _focus;
        private readonly IScreenManager _screenManager;

        private event Action OnComplete;

        public FocusService(IScreenManager screenManager) {
            _screenManager = screenManager;
            Initialized    = true;
        }

        public void Initialize() { }

        public void RegisterFocusData(TutorialFocusData data) {
            _focusDic[data.id] = data;
        }

        public void UnRegisterFocusData(TutorialFocusData data) {
            // 강제 종료 시 이미 Dispose 되어서 에러나는 경우가 있음
            // if (_focusDic == null) return;
            _focusDic.Remove(data.id);
        }

        public async UniTask StartAsync(GuideBase guide, Action onComplete = null, Action onSkip = null, CancellationToken ct = default) {
            await SafeArea(true);
            await StopAsync(false, ct);

            // UI가 열리기 전 실행 됐을 때 대기 타임
            var data = await GetRetryFocusData(guide, 100, ct);
            if (data == null) {
                onComplete?.Invoke();
                return;
            }

            if (guide is FocusGuide focusGuide) {
                _focus = await _screenManager.OpenAsync(new FocusOption() {
                                                            TutorialFocusData = data,
                                                            FocusGuide        = focusGuide
                                                        }, FocusScreenKey, ct) as ITutorialFocus;
                await SafeArea(false);
            }

            SetFocusCompleteCallBack(data, onComplete);
        }

        public async UniTask StopAsync(bool hide, CancellationToken ct = default) {
            OnComplete = null;
            if (_focus == null) return;
            await _focus.StopAsync(hide, ct);
        }

        public async UniTask<TutorialFocusData> GetRetryFocusData(GuideBase guideData, int maxRetryCount = 100, CancellationToken ct = default) {
            var               retryCount = 0;
            TutorialFocusData data       = null;
            while (retryCount <= maxRetryCount && data == null && !ct.IsCancellationRequested) {
                data = GetFocusData(guideData);
                ++retryCount;
                var isCancel = await UniTask.DelayFrame(2, cancellationToken: ct).SuppressCancellationThrow();
                if (isCancel) break;
            }
            return data;
        }

        public TutorialFocusData GetFocusData(GuideBase guide) {
            if (guide is FocusGuide focusGuide) {
                if (string.IsNullOrEmpty(focusGuide.id)) {
                    return _focusDic.FirstOrDefault(r => r.Value.Guid == focusGuide.focusGuid).Value;
                }

                return _focusDic.GetValueOrDefault(focusGuide.id);
            }
            return null;
        }

        private async UniTask SafeArea(bool on) {
            if (_screenManager == null) return;
            if (on) {
                await _screenManager.ShowSafeAreaAsync();
            }
            else {
                await _screenManager.HideSafeAreaAsync();
            }
        }

        private void SetFocusCompleteCallBack(TutorialFocusData data, Action completeCallBack, bool isShortCut = false) {
            OnComplete = completeCallBack;

            void OnCompleteEvent() {
                OnComplete?.Invoke();
                OnComplete = null;
            }

            var useGard = data.useGard;
            _focus.SetGardAlpha(useGard);

            switch (data.type) {
                case FocusType.Button: {
                    var btn = data.target as SW_GUI_BUTTON_BASE;
                    if (btn == null) {
                        OnCompleteEvent();
                        return;
                    }

                    if (isShortCut) {
                        btn.Click();
                        OnCompleteEvent();
                    }
                    else {
                        async UniTask OnClickAsyncEvent() {
                            if (BlockButton) return;
                            await SafeArea(true);
                            _focus.FocusButton.RemoveClickAsyncListener(OnClickAsyncEvent);
                            btn.Click();
                            OnCompleteEvent();
                            _focus.SetButton(false);
                        }

                        _focus.SetButton(true);
                        _focus.FocusButton.AddClickAsyncListener(OnClickAsyncEvent, false);
                    }
                }
                    break;

                case FocusType.Toggle: {
                    var tfToggle = data.target as SW_GUI_TOGGLE_BASE;
                    if (tfToggle == null) {
                        OnCompleteEvent();
                        return;
                    }

                    if (isShortCut) {
                        tfToggle.SetIsOn(true, true);
                        OnCompleteEvent();
                    }
                    else {
                        async UniTask OnClickAsyncEvent() {
                            if (BlockButton) return;
                            await SafeArea(true);
                            _focus.FocusButton.RemoveClickAsyncListener(OnClickAsyncEvent);
                            tfToggle.OnClick();
                            OnCompleteEvent();
                            _focus.SetButton(false);
                        }

                        _focus.SetButton(true);
                        _focus.FocusButton.AddClickAsyncListener(OnClickAsyncEvent);
                    }
                }
                    break;

                case FocusType.Image:
                case FocusType.None: {
                    async UniTask OnClickAsyncEvent() {
                        if (BlockButton) return;
                        await SafeArea(true);
                        _focus.FocusButton.RemoveClickAsyncListener(OnClickAsyncEvent);
                        OnCompleteEvent();
                        _focus.SetButton(false);
                    }

                    _focus.SetButton(true);
                    _focus.FocusButton.AddClickAsyncListener(OnClickAsyncEvent);
                }
                    break;
            }
        }

        public void Dispose() {
            StopAsync(true).Forget();
        }
    }
}