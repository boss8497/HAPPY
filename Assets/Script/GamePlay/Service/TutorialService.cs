using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.GamePlay.Service.Interface;
using Script.Tutorial;
using Script.Tutorial.Interface;
using SW.GUI.Base;
using VContainer.Unity;

namespace Script.GamePlay.Service {
    public class TutorialService : ITutorialService, IInitializable {
        public bool Initialized  { get; private set; }
        
        
        public void Initialize() {
            throw new System.NotImplementedException();
        }
        
        private Dictionary<string, TutorialFocusData> _focusDatas = new();
        public  bool                                  IsInitialized => _focusDatas.Count > 0;
        //서버에게 Req를 보내고 Ack를 받는 상황의 Ui에서 막아주는게 필요
        public  bool                          BlockButton   { get; set; } = false;


        //private IDialogSafeArea _safeArea;
        private ITutorialFocus _focus;
        private event Action   OnComplete;

        public void RegisterFocus(ITutorialFocus focus) {
            _focus = focus;
        }

        public void RegisterFocusData(TutorialFocusData data) {
            _focusDatas[data.id] = data;
        }

        public void UnRegisterFocusData(TutorialFocusData data) {
            _focusDatas.Remove(data.id);
        }

        public async UniTask StartAsync(GuideBase guide, Action onComplete = null, Action onSkip = null, int enumOption = 0) {
            if (_focus == null) {
                onComplete?.Invoke();
                return;
            }
            
            await StopAsync(false);

            var data = await GetRetryFocusData(guide, 100);
            if (data == null) {
                onComplete?.Invoke();
                return;
            }

            // if (guide is FocusGuide focusGuide) {
            //     var uiBase =  data.rtf.GetComponentInParent<IScreen>();
            //     
            //     if (uiBase != null) {
            //         await UniTask.WaitUntil(()=> data.rtf.gameObject.activeSelf);
            //         await UniTask.Yield();
            //     }
            //     
            //     switch ((FocusOption)enumOption) {
            //         case FocusOption.None:
            //             await _focus.SetFocusAsync(data, focusGuide);
            //             break;
            //         case FocusOption.MoveAnimation:
            //             await _focus.SetFocusAnimation(data, focusGuide);
            //             break;
            //     }
            //     SafeArea(false);
            // }
            
            SetCompleteCallBack(data, onComplete, false);
        }

        public async UniTask StopAsync(bool hide) {
            await _focus.StopAsync(hide);
            OnComplete = null;
        }

        public async UniTask ScreenHide() {
            await _focus.ScreenHide();
        }

        public async UniTask<TutorialFocusData> GetRetryFocusData(GuideBase guiedData, int maxRetryCount = 100) {
            var               retryCount = 0;
            TutorialFocusData data       = null;
            while (retryCount <= maxRetryCount && data == null) {
                data       = GetFocusData(guiedData);
                ++retryCount;
                await UniTask.DelayFrame(2);
            }
            
            return data;
        }

        public TutorialFocusData GetFocusData(GuideBase guide) {
            if (guide is FocusGuide focusGuide) {
                if (string.IsNullOrEmpty(focusGuide.id)) {
                    return _focusDatas.FirstOrDefault(r => r.Value.Guid == focusGuide.focusGuid).Value; 
                }
                
                return _focusDatas.GetValueOrDefault(focusGuide.id);
            }

            return null;
        }

        public bool IsScreenShow() {
            return _focus.IsScreenShow?.CurrentValue ?? false;
        }

        private void SafeArea(bool on) {
            // if (_safeArea == null) return;
            // if (on) {
            //     _safeArea.On();
            // }
            // else {
            //     _safeArea.Off();
            // }
        }

        private void SetCompleteCallBack(TutorialFocusData data, Action completeCallBack, bool isShortCut = false) {
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
                        
                        void OnClickEvent() {
                            if (BlockButton) return;
                            SafeArea(true);
                            _focus.FocusButton.RemoveClickListener(OnClickEvent);
                            btn.Click();
                            OnCompleteEvent();
                            _focus.SetButton(false);
                        }
                        
                        _focus.SetButton(true);
                        _focus.FocusButton.AddClickListener(OnClickEvent);
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
                        void OnClickEvent() {
                            if (BlockButton) return;
                            SafeArea(true);
                            _focus.FocusButton.RemoveClickListener(OnClickEvent);
                            tfToggle.OnClick();
                            OnCompleteEvent();
                            _focus.SetButton(false);
                        }
                        
                        _focus.SetButton(true);
                        _focus.FocusButton.AddClickListener(OnClickEvent);
                    }

                }
                    break;
                
                case FocusType.Image:
                case FocusType.None: {
                    void OnClickEvent() {
                        if (BlockButton) return;
                        SafeArea(true);
                        _focus.FocusButton.RemoveClickListener(OnClickEvent);
                        OnCompleteEvent();
                        _focus.SetButton(false);
                    }
                    _focus.SetButton(true);
                    _focus.FocusButton.AddClickListener(OnClickEvent);
                }
                    break;
            }
        }
    }
}