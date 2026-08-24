using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Script.GameInfo.Info;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Script.GamePlay.Stage;
using Script.GUI.Screen.Interface;
using Script.LifetimeScope.Locator;
using UnityEngine;
using VContainer;

namespace Script.GamePlay.Service {
    public class TutorialService : ITutorialService, IDisposable {
        private IFocusService  _focusService;
        private IScopeLocator  _scopeLocator;
        private IScreenManager _screenManager;

        public bool Initialized { get; private set; }

        private IDisposable _disposable;


        private bool                    _systemControl;
        private CancellationTokenSource _cts;

        private Queue<int> _waitQueue = new();
        private int        _currentTutorialUid;

        public TutorialInfo CurrentTutorialInfo => GameInfoManager.Instance.Get<TutorialInfo>(_currentTutorialUid);
        public bool         IsPlay              => (_cts?.IsCancellationRequested ?? true) == false;

        private readonly Subject<Guid> _onDialogEndedSubject = new();


        public TutorialService(
            IFocusService  focusService,
            IScopeLocator  scopeLocator,
            IScreenManager screenManager
        ) {
            _focusService  = focusService;
            _scopeLocator  = scopeLocator;
            _screenManager = screenManager;
            Initialized    = true;
        }

        private IStageManager GetStageManager() {
            var child = _scopeLocator.GetLastChildScope();
            return child.Container.Resolve<IStageManager>();
        }

        private bool IsInitialized() {
            return Initialized && (_focusService?.Initialized ?? false);
        }

        public void StartTutorial(int uid) {
            if (_waitQueue.Contains(uid) || _currentTutorialUid == uid) return;
            var guideInfo = GameInfoManager.Instance.Get<TutorialInfo>(uid);
            if (guideInfo == null) {
                return;
            }

            StartTutorial(guideInfo);
        }

        public void StartTutorial(TutorialInfo tutorialInfo) {
            if (_waitQueue.Contains(tutorialInfo.UID) || _currentTutorialUid == tutorialInfo.UID) {
                return;
            }

            // 상시 Update Loop로 뺴자
            _waitQueue.Enqueue(tutorialInfo.UID);

            if (IsPlay == false) {
                _cts = new();
                var token = _cts.Token;
                UpdateLoop(token).Forget();
            }
        }
        
        public void StopTutorial() {
            SetSafeArea(false);
            if (_cts is { IsCancellationRequested: false }) {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async UniTask UpdateLoop(CancellationToken ct = default) {
            var isCancel = await UniTask.WaitUntil(IsInitialized, cancellationToken: ct).SuppressCancellationThrow();
            if (isCancel) return;

            while (!ct.IsCancellationRequested && _waitQueue.Count > 0) {
                _currentTutorialUid = _waitQueue.Dequeue();
                var tutorialInfo = CurrentTutorialInfo;
                
                if (tutorialInfo.systemControl) {
                    SetSystemControl(true);
                }

                //모든 스크린을 종료한 후 실행하는 Dialog
                if (tutorialInfo.allCloseScreen) {
                    await _screenManager.CloseAllAsync();
                    isCancel = await UniTask.DelayFrame(2, cancellationToken: ct).SuppressCancellationThrow();
                    if (isCancel) {
                        SetSafeArea(false);
                        Debug.LogError($"Tutorial UpdateLoop is Cancelled.");
                        break;
                    }
                }


                var dialogCount = tutorialInfo.sets.Length;
                for (int i = 0; i < dialogCount; i++) {
                    var guide     = tutorialInfo.sets[i];
                    var nextGuide = i + 1 >= dialogCount ? null : tutorialInfo.sets[i + 1];

                    if (ct.IsCancellationRequested) break;

                    //포커스
                    if (guide is FocusGuide fGuide) {
                        var focusData = await _focusService.GetRetryFocusData(fGuide, ct: ct);
                        if (focusData == null) {
                            continue;
                        }

                        var retryCount    = 0;
                        var retryMaxCount = 100;
                        while (retryCount <= retryMaxCount && focusData.rtf.gameObject.activeInHierarchy == false) {
                            ++retryCount;
                            await UniTask.DelayFrame(2, cancellationToken: ct);
                        }

                        if (retryCount > retryMaxCount && focusData.rtf.gameObject.activeInHierarchy == false) {
                            Debug.LogError($"다이어로그 [{tutorialInfo.UID}]의 [{focusData.id}] 포커스가 켜져있지 않습니다.");
                            continue;
                        }

                        var focusComplete = false;

                        SetSafeArea(true);
                        await _focusService.StartFocusAsync(guide, () => { focusComplete = true; }, ct: ct);
                        SetSafeArea(false);

                        isCancel = await UniTask.WaitUntil(() => focusComplete, cancellationToken: ct).SuppressCancellationThrow();
                        if (isCancel) {
                            SetSafeArea(false);
                            Debug.LogError($"Tutorial UpdateLoop is Cancelled.");
                            break;
                        }

                        SetSafeArea(true);
                        isCancel = await UniTask.DelayFrame(2, PlayerLoopTiming.FixedUpdate, ct).SuppressCancellationThrow();
                        if (isCancel) {
                            SetSafeArea(false);
                            Debug.LogError($"Tutorial UpdateLoop is Cancelled.");
                            break;
                        }

                        await _focusService.StopFocusAsync(nextGuide is not FocusGuide, ct);
                    }
                }

                if (tutorialInfo.systemControl) {
                    SetSystemControl(false);
                }
                _currentTutorialUid = -1;
            }
            
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void SetSafeArea(bool isOn) {
            if (isOn) {
                _screenManager.ShowSafeAreaAsync();
            }
            else {
                _screenManager.HideSafeAreaAsync();
            }
        }

        private void SetSystemControl(bool enable) {
            var stageManager = GetStageManager();
            if (stageManager == null) return;
            _systemControl = enable;
            stageManager.AddState(StageState.SystemControl);
        }

        public void Dispose() {
            _disposable?.Dispose();
            _cts?.Dispose();
            _onDialogEndedSubject?.Dispose();
        }
    }
}