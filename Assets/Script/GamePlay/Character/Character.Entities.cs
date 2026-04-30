using System.Linq;
using Script.GameInfo.Character;
using Script.GamePlay.ECS.Component;
using Unity.Entities;

namespace Script.GamePlay.Character {
    public partial class Character {
        private void InitializeEntity() {
            if (_unitManager == null)
                return;

            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;
            EnsureComponents(entityManager, entity);

            //RegisterDefaultHitbox(characterInfo, presetRanges, presetShapes);
            //RegisterStateHitboxes(characterInfo, presetRanges, presetShapes);
        }

        private void EnsureComponents(EntityManager entityManager, Entity entity) {
            // Enable 셋팅
            if (entityManager.HasComponent<UnitDieEnable>(entity) == false) {
                entityManager.AddComponentData<UnitDieEnable>(entity, new());
            }

            SetEnabledTag<UnitDieEnable>(false);

            if (entityManager.HasComponent<UnitRunningEnable>(entity) == false) {
                entityManager.AddComponentData<UnitRunningEnable>(entity, new());
            }

            SetEnabledTag<UnitRunningEnable>(false);

            if (entityManager.HasComponent<UnitJumpingEnable>(entity) == false) {
                entityManager.AddComponentData<UnitJumpingEnable>(entity, new());
            }

            SetEnabledTag<UnitJumpingEnable>(false);

            if (entityManager.HasComponent<UnitCollisionResultEnable>(entity) == false) {
                entityManager.AddComponentData<UnitCollisionResultEnable>(entity, new());
            }
            SetEnabledTag<UnitCollisionResultEnable>(false);
            
            if (entityManager.HasComponent<UnitCollisionEnable>(entity) == false) {
                entityManager.AddComponentData<UnitCollisionEnable>(entity, new());
            }
            SetEnabledTag<UnitCollisionEnable>(true);

            if (entityManager.HasComponent<UnitSystemControlEnable>(entity) == false) {
                entityManager.AddComponentData<UnitSystemControlEnable>(entity, new());
            }

            SetEnabledTag<UnitSystemControlEnable>(SystemControl?.CurrentValue ?? false);


            //HitBox
            if (entityManager.HasComponent<HitBoxData>(entity) == false) {
                entityManager.AddComponentData<HitBoxData>(entity, new());
            }

            if (entityManager.HasBuffer<UnitCollisionResult>(entity) == false) {
                entityManager.AddBuffer<UnitCollisionResult>(entity);
            }

            if (entityManager.HasBuffer<UnitCollisionDelay>(entity) == false) {
                entityManager.AddBuffer<UnitCollisionDelay>(entity);
            }

            // Data 셋팅
            if (entityManager.HasComponent<RunningData>(entity) == false) {
                entityManager.AddComponentData(entity, new RunningData());
            }

            if (entityManager.HasComponent<JumpInputData>(entity) == false) {
                entityManager.AddComponentData(entity, new JumpInputData());
            }

            if (entityManager.HasComponent<JumpingData>(entity) == false) {
                entityManager.AddComponentData(entity, new JumpingData());
            }
        }

        private void SetEnabledTag<T>(bool enable) where T : IComponentData, IEnableableComponent {
            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;
            if (entityManager.HasComponent<T>(entity) == false)
                return;

            entityManager.SetComponentEnabled<T>(entity, enable);
        }
    }
}