using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Script.GamePlay.Input {
    public class PlayerControls : IPlayerControls {
        private readonly PlayerControlMap _controlMap = new();

        private CancellationTokenSource _cts;
        private bool                    _initialized;
        private bool                    _disposed;

        private Vector2 _move;
        private bool    _jumpPressed;
        private bool    _jumpReleased;
        private bool    _jumpHeld;
        private bool    _hasAnyInput;

        public Vector2 Move         => _move;
        public bool    JumpPressed  => _jumpPressed;
        public bool    JumpHeld     => _jumpHeld;
        public bool    JumpReleased => _jumpReleased;
        public bool    HasMoveInput => _move.sqrMagnitude > 0.0001f;
        public bool    HasAnyInput  => _hasAnyInput;

        public PlayerControls() {
            Initialize();
        }

        public void Initialize() {
            if (_initialized)
                return;

            _initialized = true;
            _cts         = new CancellationTokenSource();

            _controlMap.Enable();

            RunUpdateLoopAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid RunUpdateLoopAsync(CancellationToken token) {
            try {
                while (token.IsCancellationRequested == false) {
                    UpdateInputState();

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException) {
                // 정상 종료
            }
        }

        private void UpdateInputState() {
            // edge 값은 이번 업데이트 기준으로 다시 계산
            _jumpPressed  = false;
            _jumpReleased = false;
            _hasAnyInput  = false;

            var player = _controlMap.Player;

            _move     = player.Move.ReadValue<Vector2>();
            _jumpHeld = player.Jump.IsPressed();

            if (_move.sqrMagnitude > 0.0001f) {
                _hasAnyInput = true;
                Debug.Log($"Move: {_move}");
            }

            if (player.Jump.WasPressedThisFrame()) {
                _jumpPressed = true;
                _hasAnyInput = true;
                Debug.Log($"Jump!!");
            }

            if (player.Jump.WasReleasedThisFrame()) {
                _jumpReleased = true;
                _hasAnyInput  = true;
                Debug.Log($"Release Jump!!");
            }

            if (_jumpHeld)
                _hasAnyInput = true;
        }

        public bool ConsumeJumpPressed() {
            if (_jumpPressed == false)
                return false;

            _jumpPressed = false;
            return true;
        }

        public bool ConsumeJumpReleased() {
            if (_jumpReleased == false)
                return false;

            _jumpReleased = false;
            return true;
        }

        public void Dispose() {
            if (_disposed)
                return;

            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _controlMap.Disable();
        }
    }
}