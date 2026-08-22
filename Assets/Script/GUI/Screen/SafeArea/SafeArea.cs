using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameTimer;
using Script.GUI.ScreenData.Interface;
using UnityEngine;
using VContainer;

namespace Script.GUI.Screen.SafeArea {
    public class SafeArea : Screen {
        private IGameTimer _gameTimer;

        [SerializeField] private float autoBackTimer = 5.0f;

        private CancellationTokenSource _cts;

        [Inject]
        public void InjectSelf(IGameTimer gameTimer) {
            _gameTimer = gameTimer;
        }

        public override UniTask OpenInternal(IScreenOption screenOption, CancellationToken ct = default) {
            _cts = new();
            AutoBack(_cts.Token).Forget();
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            ReleaseAuto();
            return UniTask.CompletedTask;
        }

        private async UniTask AutoBack(CancellationToken ct) {
            var timer    = 0f;
            var isCancel = false;
            while (timer < autoBackTimer && !ct.IsCancellationRequested) {
                timer    += _gameTimer?.UnscaledDeltaTime ?? Time.unscaledDeltaTime;
                isCancel =  await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct).SuppressCancellationThrow();
                if (isCancel) break;
            }

            if (!isCancel) {
                BackAsync().Forget();
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