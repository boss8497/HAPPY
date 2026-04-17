using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace Script.GameTimer {
    public class GameTimer : IGameTimer, IInitializable, IDisposable {
        public float Elapsed      { get; private set; } = 0f;
        public float FixedElapsed { get; private set; } = 0f;
        public float DeltaTime    { get; private set; } = 0f;
        public float FixedTime    { get; private set; } = 0f;
        public bool  IsPaused     { get; private set; }

        private CancellationTokenSource _cts;

        public void Initialize() {
            _cts = new();
            UpdateTimer(_cts.Token).Forget();
            UpdateFixedTimer(_cts.Token).Forget();
        }

        private async UniTask UpdateTimer(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                DeltaTime =  IsPaused ? 0f : Time.unscaledDeltaTime;
                Elapsed   += DeltaTime;

                var isCancel = await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct).SuppressCancellationThrow();
                if (isCancel) {
                    break;
                }
            }
        }

        private async UniTask UpdateFixedTimer(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                FixedTime    =  IsPaused ? 0f : Time.fixedUnscaledTime;
                FixedElapsed += FixedTime;

                var isCancel = await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancellationToken: ct).SuppressCancellationThrow();
                if (isCancel) {
                    break;
                }
            }
        }

        public void Pause() {
            SetPause(true);
        }

        public void Resume() {
            SetPause(false);
        }

        private void SetPause(bool paused) {
            IsPaused = paused;
        }

        private void Stop() {
            if (_cts is { IsCancellationRequested: false }) {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        public void Dispose() {
            Stop();
        }
    }
}