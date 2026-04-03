using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.GameInfo.Table;
using Script.Utility.Runtime;
using Script.GameInfo.Character;
using Script.GamePlay.ECS.Component;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Script.GamePlay.Character {
    public partial class Character {
        private ConfigurationInfo _config;

        private CancellationTokenSource _jumpCts;

        private void InitializeAction() {
            _config = GameInfoManager.Instance.Config;
        }

        public void Run() {
            if ((Running?.CurrentValue ?? false)) {
                return;
            }
            ReleaseRunning();
            
            EnableRunning();
            AddState(CharacterState.Running);
        }
        
        private void EnableRunning() {
            if (_unitManager == null)
                return;

            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;
            if (entityManager.HasComponent<RunningData>(entity) == false) {
                throw new Exception($"Entity {entity} does not have RunningData component");
            }

            entityManager.SetComponentData(entity, new RunningData {
                Direction = Vector3.right, // 오른쪽으로 고정
                Speed     = (float)Status.Spd,
            });

            entityManager.SetComponentEnabled<RunningData>(entity, true);
        }

        private void DisableRunning() {
            if (_unitManager == null)
                return;

            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;

            if (entityManager.HasComponent<RunningData>(entity) == false)
                return;

            entityManager.SetComponentEnabled<RunningData>(entity, false);
        }
        
        private void Update() {
            SyncJumpInputEntity();
            SyncJumpResultEntity();
        }
        
        private void SyncJumpInputEntity() {
            if ((Jumping?.CurrentValue ?? false) == false) return;
            if (_unitManager == null) return;
            if (_unitManager.TryGetEntity(this, out var entity) == false) return;

            var entityManager = _stageEntityWorld.EntityManager;

            if (entityManager.HasComponent<JumpInputData>(entity) == false) return;

            var input = entityManager.GetComponentData<JumpInputData>(entity);

            input.Held = (byte)(PlayerControls.JumpHeld ? 1 : 0);

            // 버튼을 뗀 순간은 놓치기 쉬우니 latch처럼 1회 기록
            if (PlayerControls.JumpReleased) {
                input.ReleaseRequested = 1;
            }

            entityManager.SetComponentData(entity, input);
        }
        
        private void SyncJumpResultEntity() {
            if ((Jumping?.CurrentValue ?? false) == false) return;
            if (_unitManager == null) return;
            if (_unitManager.TryGetEntity(this, out var entity) == false) return;

            var entityManager = _stageEntityWorld.EntityManager;

            if (entityManager.HasComponent<JumpResultData>(entity) == false) return;

            var result = entityManager.GetComponentData<JumpResultData>(entity);
            if (result.Landed == 0) return;

            result.Landed = 0;
            entityManager.SetComponentData(entity, result);

            ReleaseJumping();
        }

        public void Jump() {
            if ((Jumping?.CurrentValue ?? false)) {
                return;
            }

            ReleaseJumping();
            AddState(CharacterState.Jumping);
            EnableJumping();
        }
        
        private void EnableJumping() {
            if (_unitManager == null) return;
            if (_unitManager.TryGetEntity(this, out var entity) == false) return;

            var entityManager = _stageEntityWorld.EntityManager;

            EnsureJumpingComponents(entityManager, entity);

            entityManager.SetComponentData(entity, new JumpInputData {
                Held             = (byte)(PlayerControls.JumpHeld ? 1 : 0),
                ReleaseRequested = 0,
            });

            entityManager.SetComponentData(entity, new JumpResultData {
                Landed = 0,
            });

            entityManager.SetComponentData(entity, new JumpingData {
                GroundY         = transform.position.y,
                CurrentJumpTime = _config.maxJumpTime,
                MaxJumpTime     = _config.maxJumpTime,
                Gravity         = _config.gravity,
                FallGravity     = _config.fallGravity,
                Timer           = 0f,
                JumpVelocity    = Convert.ToSingle(Status.Jump),
            });

            entityManager.SetComponentEnabled<JumpingData>(entity, true);
        }
        
        private void EnsureJumpingComponents(EntityManager entityManager, Entity entity) {
            if (entityManager.HasComponent<JumpInputData>(entity) == false) {
                entityManager.AddComponentData(entity, new JumpInputData {
                    Held             = 0,
                    ReleaseRequested = 0,
                });
            }

            if (entityManager.HasComponent<JumpResultData>(entity) == false) {
                entityManager.AddComponentData(entity, new JumpResultData {
                    Landed = 0,
                });
            }

            if (entityManager.HasComponent<JumpingData>(entity) == false) {
                entityManager.AddComponentData(entity, new JumpingData {
                    GroundY         = 0f,
                    CurrentJumpTime = 0f,
                    MaxJumpTime     = 0f,
                    Gravity         = 0f,
                    FallGravity     = 0f,
                    Timer           = 0f,
                    JumpVelocity    = 0f,
                });

                entityManager.SetComponentEnabled<JumpingData>(entity, false);
            }
        }
        
        private void DisableJumping() {
            if (_unitManager == null) return;
            if (_unitManager.TryGetEntity(this, out var entity) == false) return;

            var entityManager = _stageEntityWorld.EntityManager;

            if (entityManager.HasComponent<JumpingData>(entity)) {
                entityManager.SetComponentEnabled<JumpingData>(entity, false);
            }

            if (entityManager.HasComponent<JumpInputData>(entity)) {
                entityManager.SetComponentData(entity, new JumpInputData {
                    Held             = 0,
                    ReleaseRequested = 0,
                });
            }
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
                    float dt = Time.fixedDeltaTime;

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
                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cts);
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
            DisableJumping();
            RemoveState(CharacterState.Jumping);
        }

        private void ReleaseRunning() {
            DisableRunning();
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