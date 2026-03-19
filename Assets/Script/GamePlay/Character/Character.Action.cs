using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.GameInfo.Table;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Character {
    public partial class Character {
        private ConfigurationInfo _config;

        private CancellationTokenSource _jumpCts;
        private CancellationTokenSource _runCts;

        private void InitializeAction() {
            _config = GameInfoManager.Instance.Config;
        }

        public void Run() {
            if (Running) {
                return;
            }

            ReleaseRunning();
            AddState(CharacterState.Running);
            _runCts = new();
            RunningAsync(_runCts.Token).Forget();
        }

        private async UniTaskVoid RunningAsync(CancellationToken cts) {
            var spd = (float)Status.Spd;
            try {
                while (cts.IsCancellationRequested == false) {
                    var position = transform.position;
                    position.x         += spd * Time.deltaTime;
                    transform.position =  position;

                    await UniTask.Yield(PlayerLoopTiming.Update, cts);
                }
            }
            catch (OperationCanceledException) { }
            finally {
                ReleaseRunning();
            }
        }

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
                var groundY         = transform.position.y;
                var currentJumpTime = _config.maxJumpTime;
                var maxJumpTime     = _config.maxJumpTime;
                var gravity         = _config.gravity;
                var timer           = 0f;

                // Status.Jump를 점프 시작 힘으로 사용
                var jumpVelocity = Convert.ToSingle(Status.Jump);

                while (cts.IsCancellationRequested == false) {
                    float dt = Time.deltaTime;

                    // 점프 유지 시간 증가
                    if (PlayerControls.JumpHeld && currentJumpTime < maxJumpTime) {
                        currentJumpTime += dt;
                        if (currentJumpTime > maxJumpTime)
                            currentJumpTime = maxJumpTime;
                    }

                    // 버튼을 떼면 최소 시간까지만 상승 허용
                    if (PlayerControls.JumpReleased) {
                        currentJumpTime = Mathf.Min(currentJumpTime, timer);
                    }

                    // 상승 구간
                    if (timer < currentJumpTime && jumpVelocity > 0f) {
                        // 유지 중일 때는 천천히 감쇠
                        jumpVelocity -= gravity * dt * 0.5f;
                    }
                    else {
                        // 하강 구간
                        jumpVelocity -= gravity * _config.fallGravity * dt;
                    }

                    var position = transform.position;
                    position.y += jumpVelocity * dt;

                    // 바닥 도달
                    if (position.y <= groundY && timer > 0f) {
                        position.y         = groundY;
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
                ReleaseJumping();
            }
        }

        private void ReleaseAction() {
            ReleaseRunning();
            ReleaseJumping();
        }

        private void ReleaseJumping() {
            _jumpCts?.Cancel();
            _jumpCts?.Dispose();
            _jumpCts = null;
            RemoveState(CharacterState.Jumping);
        }

        private void ReleaseRunning() {
            _runCts?.Cancel();
            _runCts?.Dispose();
            _runCts = null;
            RemoveState(CharacterState.Running);
        }

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