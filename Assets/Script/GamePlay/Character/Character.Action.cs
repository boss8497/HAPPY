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
        private ConfigurationInfo       _config;
        
        /// <summary>
        /// 캐릭터 행동을 정의 하는 cs 파일
        /// 코딩 규칙
        /// - 행동과 관련된 메서드 작성
        /// - 행동을 변경을 감지하고 시작 및 중지 하는 부분은 Reactive로 구현!!
        /// </summary>
        private void InitializeAction() {
            _config = GameInfoManager.Instance.Config;
            UpdateStatus();
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
            
            ApplyCollision(otherCharacter);
            
            switch (otherCharacter.CharacterInfo.type) {
                case CharacterType.Heart:
                    ApplyHeart(otherCharacter);
                    break;
                
                case CharacterType.Buff:
                    ApplyBuff(otherCharacter.CharacterInfo.buffUids);
                    break;
                
                case CharacterType.Score:
                    _stageManager.AddItemScore((float)otherCharacter.Status.Score);
                    //SetEnabledTag<UnitCollisionDelayTag>(false);
                    break;
                
                case CharacterType.Obstacle:
                    // 캐릭터가 장애물에 Collision 됐을 때만 일단 설정
                    AddState(CharacterState.Collision);
                    break;
            }
        }

        private void ApplyHeart(Character otherCharacter){
             var heartValue = otherCharacter.Status.Heart;
             if (heartValue <= 0d) return;
             ApplyHealth(heartValue);
        }

        // Collision은 Def의 영향을 안받기 때문에 일단 따로 계산해 줌
        private void ApplyCollision(Character otherCharacter) {
            var collisionDamage = otherCharacter.Status.Collision;
            if (collisionDamage <= 0d) return;
            
            Debug.LogError($"충돌했다고해!!! me: {CharacterInfo.Name} other: {otherCharacter.CharacterInfo.Name} damage: {collisionDamage}");
            ApplyHealth(-collisionDamage);
        }
        
        public void ApplyDamage() {
        }

        private void ApplyHealth(double health) {
            var currentHealth = Health.CurrentValue;
            var newHealth     = currentHealth + health;
            if (newHealth < 0)
                newHealth = 0;
            Health.OnNext(newHealth);
        }

        private void UpdateStatus() {
            UpdateRunningStatus();
        }

        private void UpdateRunningStatus() {
            // 달리기 속도가 0이면 데이터 셋팅 안함
            if (Status.Spd <= 0) {
                return;
            }
            
            if (_unitManager == null)
                return;

            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;
            if (entityManager.HasComponent<RunningData>(entity) == false) {
                throw new Exception($"Entity {entity} does not have RunningData component");
            }

            entityManager.SetComponentData(entity, new RunningData {
                Direction = Vector3.right, // 오른쪽으로 고정 나중에 왼쪽으로 달려오는 캐릭터를 만들기 위해 CharacterInfo에 데이터 셋팅하면 좋을듯!
                Speed     = (float)Status.Spd,
            });
        }
        
        // HitBox Sync
        private void SyncHitbox(CharacterState sate) {
            if (_unitManager == null)
                return;

            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;

            var stateHitbox = CharacterInfo.hitboxes.FirstOrDefault(r => r.state == sate);
            entityManager.SetComponentData(entity, new HitBoxData(stateHitbox == null ? CharacterInfo.hitbox : stateHitbox.hitbox));
        }
        
        #endregion


        #region Jumping
        public void SyncJumpEntity() {
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
        

        private void ResetJumpingStatus() {
            // 점프가 0이면 데이터 셋팅 안함
            if (Status.Jump <= 0) {
                return;
            }
            
            if (_unitManager == null || 
                _unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;
            
            entityManager.SetComponentData(entity, new JumpInputData {
                Held             = (byte)(PlayerControls.JumpHeld ? 1 : 0),
                ReleaseRequested = 0,
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
        }
        #endregion

        private void ReleaseAction() {
            RemoveState(CharacterState.Running);
            RemoveState(CharacterState.Jumping);
        }

        public float SetAnimation(string animationName, bool loop = false, bool hasExit = false) {
            if (SkeletonAnimation == null) {
                //Debug.LogError($"SkeletonAnimation is null. [{_characterInfo?.Name}]");
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