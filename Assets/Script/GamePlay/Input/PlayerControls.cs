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

        // OnScreenButton은 컨트롤 패스마다 별도 가상 Device를 새로 만들어서(InputSystem.AddDevice)
        // 이미 Enable된 액션맵에 Added 이벤트를 일으켜 Input System 내부 재해석 버그를 유발하므로,
        // 모바일 Jump 버튼은 InputAction 바인딩을 거치지 않고 이 플래그를 직접 조작한다.
        private bool _uiJumpHeld;
        private bool _uiJumpPressRequested;
        private bool _uiJumpReleaseRequested;

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
            while (token.IsCancellationRequested == false) {
                UpdateInputState();

                var isCancel = await UniTask.Yield(PlayerLoopTiming.Update, token)
                                            .SuppressCancellationThrow();
                if (isCancel) break;
            }
        }

        private void UpdateInputState() {
            // edge 값은 이번 업데이트 기준으로 다시 계산
            _jumpPressed  = false;
            _jumpReleased = false;
            _hasAnyInput  = false;

            var player = _controlMap.Player;

            _move     = player.Move.ReadValue<Vector2>();
            _jumpHeld = player.Jump.IsPressed() || _uiJumpHeld;

            if (_move.sqrMagnitude > 0.0001f) {
                _hasAnyInput = true;
            }

            if (IsJumpPressed()) {
                _jumpPressed = true;
                _hasAnyInput = true;
            }

            if (IsJumpReleased()) {
                _jumpReleased = true;
                _hasAnyInput  = true;
            }

            if (_jumpHeld)
                _hasAnyInput = true;
        }

        private bool IsJumpPressed() {
            return _controlMap.Player.Jump.WasPressedThisFrame() || ConsumeUiJumpPressRequest();
        }

        private bool IsJumpReleased() {
            return _controlMap.Player.Jump.WasReleasedThisFrame() || ConsumeUiJumpReleaseRequest();
        }

        private bool ConsumeUiJumpPressRequest() {
            if (_uiJumpPressRequested == false) return false;
            _uiJumpPressRequested = false;
            return true;
        }

        private bool ConsumeUiJumpReleaseRequest() {
            if (_uiJumpReleaseRequested == false) return false;
            _uiJumpReleaseRequested = false;
            return true;
        }

        public void PressJump() {
            _uiJumpHeld           = true;
            _uiJumpPressRequested = true;
        }

        public void ReleaseJump() {
            _uiJumpHeld             = false;
            _uiJumpReleaseRequested = true;
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