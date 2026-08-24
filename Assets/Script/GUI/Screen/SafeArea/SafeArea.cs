using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameTimer;
using Script.GUI.ScreenData;
using Script.GUI.ScreenData.Interface;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Script.GUI.Screen.SafeArea {
    public class SafeArea : Screen {
        private IGameTimer _gameTimer;
        private float      _maxTime;

        [SerializeField] private float defaultTimer = 5.0f;

        private CancellationTokenSource _cts;

        [Inject]
        public void InjectSelf(IGameTimer gameTimer) {
            _gameTimer = gameTimer;
        }

        public override UniTask OpenInternal(IScreenOption screenOption, CancellationToken ct = default) {
            Run(screenOption);
            return UniTask.CompletedTask;
        }

        public override UniTask OpenChangeOptionAsync(IScreenOption screenOption, CancellationToken ct = default) {
            Run(screenOption);
            return UniTask.CompletedTask;
        }

        private void Run(IScreenOption screenOption) {
            ReleaseAuto();
            _maxTime = screenOption is SafeAreaOption option ? option.Time : defaultTimer;
            _cts     = new();
            AutoBack().Forget();
        }

        public override UniTask CloseInternal() {
            ReleaseAuto();
            return UniTask.CompletedTask;
        }

        private async UniTask AutoBack() {
            var timer    = 0f;
            var isCancel = false;
            while (timer < _maxTime && !_cts.Token.IsCancellationRequested) {
                timer    += _gameTimer?.UnscaledDeltaTime ?? Time.unscaledDeltaTime;
                isCancel =  await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: _cts.Token).SuppressCancellationThrow();
                if (isCancel) break;
            }

            if (!isCancel) {
                await CloseAsync(true, _cts.Token);
            }
        }

        private void ReleaseAuto() {
            if (_cts != null) {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new();
            }
        }
    }
}