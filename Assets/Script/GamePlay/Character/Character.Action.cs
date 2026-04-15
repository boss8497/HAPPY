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

        private void InitializeAction() {
            _config = GameInfoManager.Instance.Config;
        }

        #region Collision
        public void Collision(long otherUid) {
            if(_unitManager.TryGetUnit(otherUid, out var otherUnit) == false) {
                throw new ArgumentException($"충돌한 유닛을 찾을 수 없습니다. UID: {otherUid}");
            }
            
            var otherCharacter = otherUnit as Character;
            if (otherCharacter == null) {
                throw new InvalidCastException($"충돌한 유닛이 Character 타입이 아닙니다. UID: {otherUid}");
            }

            
            Debug.LogError($"충돌했다고해!!! {otherCharacter.name}");
            ApplyCollisionDamage(otherCharacter);
            
            switch (otherCharacter.CharacterInfo.type) {
                case CharacterType.AddHp:
                    break;
                
                case CharacterType.Buff:
                    break;
                
                case CharacterType.Score:
                    _stageManager.AddItemScore((float)otherCharacter.Status.Score);
                    break;
                
                // case CharacterType.Character:
                // case CharacterType.Obstacle:
                //     ApplyCollisionDamage(otherCharacter);
                //     break;
            }
        }

        private void ApplyBuff() {
        }

        // Collision은 Def의 영향을 안받기 때문에 일단 따로 계산해 줌
        private void ApplyCollisionDamage(Character otherCharacter) {
            var currentHealth = Health.CurrentValue;
            var newHealth = currentHealth - otherCharacter.Status.Collision;
            if (newHealth < 0)
                newHealth = 0;
            Health.OnNext(newHealth);
        }
        
        public void ApplyDamage() {
        }
        #endregion
        

        #region Running

        public void Run() {
            if (Running?.CurrentValue ?? false) {
                return;
            }

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

        public void DisableRunning() {
            if (_unitManager == null)
                return;

            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;

            if (entityManager.HasComponent<RunningData>(entity) == false)
                return;

            entityManager.SetComponentEnabled<RunningData>(entity, false);
        }

        #endregion


        #region Jumping
        public void SyncJumpInputEntity() {
            if (_unitManager == null || 
                (Jumping?.CurrentValue ?? false) == false || 
                _unitManager.TryGetEntity(this, out var entity) == false
            ) return;

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

        public void SyncJumpResultEntity() {
            if ((Jumping?.CurrentValue ?? false) == false ||
                _unitManager == null || 
                _unitManager.TryGetEntity(this, out var entity) == false) return;

            var entityManager = _stageEntityWorld.EntityManager;

            if (entityManager.HasComponent<JumpResultData>(entity) == false) return;

            var result = entityManager.GetComponentData<JumpResultData>(entity);
            if (result.Landed == 0) return;

            result.Landed = 0;
            entityManager.SetComponentData(entity, result);

            ReleaseJumping();
        }

        public void Jump() {
            if (Jumping?.CurrentValue ?? false) {
                return;
            }
            
            AddState(CharacterState.Jumping);
            EnableJumping();
        }

        private void EnableJumping() {
            if (_unitManager == null || 
                _unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;
            
            entityManager.SetComponentData(entity, new JumpInputData {
                Held             = (byte)(PlayerControls.JumpHeld ? 1 : 0),
                ReleaseRequested = 0,
            });

            entityManager.SetComponentData(entity, new JumpResultData {
                Landed = 0,
            });

            entityManager.SetComponentData(entity, new JumpingData {
                GroundY         = transform.position.y,
                CurrentJumpTime = 0f,
                MaxJumpTime     = _config.maxJumpTime,
                MinJumpTime     = _config.minJumpTime,
                Gravity         = _config.gravity,
                FallGravity     = _config.fallGravity,
                Timer           = 0f,
                JumpVelocity    = Convert.ToSingle(Status.Jump),
            });

            entityManager.SetComponentEnabled<JumpingData>(entity, true);
        }

        private void DisableJumping() {
            if (_unitManager == null || 
                _unitManager.TryGetEntity(this, out var entity) == false) return;

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

        #endregion

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
                Debug.LogError($"SkeletonAnimation is null. [{_characterInfo?.Name}]");
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