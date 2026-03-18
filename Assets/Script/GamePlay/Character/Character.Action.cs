using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Character {
    public partial class Character {
        private float _minJumpTime = 0.5f;
        private float _maxJumpTime = 1.0f;
        private float _gravity     = 20.0f;

        private float _currentJumpTime = 0.0f;
        private float _jumpVelocity    = 0.0f;

        private float _fallGravityMultiplier = 1.5f;
        private float _groundY;

        private CancellationTokenSource _jumpCts;


        public void Jump() {
            if (Jumping) {
                return;
            }

            ReleaseJumping();
            AddState(CharacterState.Jumping);
            _jumpCts = new();
            JumpingAsync(_jumpCts.Token).Forget();
        }

        private async UniTaskVoid JumpingAsync(CancellationToken cts) {
            try {
                _groundY = transform.position.y;

                _currentJumpTime = _minJumpTime;
                var timer = 0f;

                // Status.Jump를 점프 시작 힘으로 사용
                _jumpVelocity = Convert.ToSingle(Status.Jump);

                while (cts.IsCancellationRequested == false) {
                    float dt = Time.deltaTime;

                    // 점프 유지 시간 증가
                    if (PlayerControls.JumpHeld && _currentJumpTime < _maxJumpTime) {
                        _currentJumpTime += dt;
                        if (_currentJumpTime > _maxJumpTime)
                            _currentJumpTime = _maxJumpTime;
                    }

                    // 버튼을 떼면 최소 시간까지만 상승 허용
                    if (PlayerControls.JumpReleased) {
                        _currentJumpTime = Mathf.Min(_currentJumpTime, timer);
                    }

                    // 상승 구간
                    if (timer < _currentJumpTime && _jumpVelocity > 0f) {
                        // 유지 중일 때는 천천히 감쇠
                        _jumpVelocity -= _gravity * dt * 0.5f;
                    }
                    else {
                        // 하강 구간
                        _jumpVelocity -= _gravity * _fallGravityMultiplier * dt;
                    }

                    var position = transform.position;
                    position.y += (float)_jumpVelocity * dt;

                    // 바닥 도달
                    if (position.y <= _groundY && timer > 0f) {
                        position.y         = _groundY;
                        transform.position = position;
                        break;
                    }

                    transform.position = position;

                    timer += dt;
                    await UniTask.Yield(PlayerLoopTiming.Update, cts);
                }
            }
            catch (OperationCanceledException) { }
            finally {
                _jumpVelocity    = 0f;
                _currentJumpTime = 0f;

                ReleaseJumping();
            }
        }

        private void ReleaseJumping() {
            _jumpCts?.Cancel();
            _jumpCts?.Dispose();
            _jumpCts = null;
            RemoveState(CharacterState.Jumping);
        }


        public void ReleaseAction() { }

        public float SetAnimation(string animationName, bool loop = false, bool hasExit = false) {
            if (SkeletonAnimation == null) {
                Debug.LogError($"SkeletonAnimation is null");
                return 0f;
            }

            if ((SpineAnimationNames?.Any(a => a == animationName) ?? true) == false) {
                return 0f;
            }

            //애니메이션이 같으면
            var currentAnimation = SkeletonAnimation.AnimationState.GetCurrent(0);
            if (currentAnimation != null && animationName.Equals(currentAnimation.Animation.Name) && currentAnimation.Loop == loop) {
                return 0;
            }

            return SkeletonAnimation.StartAnimation(animationName, loop, hasExit)?.AnimationEnd ?? 0;
        }
    }
}