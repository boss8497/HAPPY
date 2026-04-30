using Script.GameInfo.Character;
using Script.GamePlay.ECS.Component;
using Unity.Entities;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class StageSyncSystem : SystemBase {
        protected override void OnUpdate() {
            var cameraData      = SystemAPI.GetSingletonRW<CameraData>();
            var camera          = cameraData.ValueRO.Camera.Value;
            var cameraTransform = cameraData.ValueRO.Camera.Value.transform;
            var outSidePosX     = cameraTransform.position.x - camera.orthographicSize * camera.aspect;

            foreach (var (transformRef, unitData, hitBoxData)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRW<UnitData>, RefRO<HitBoxData>>()
                                 .WithAll<UnitEntityTag>()
                                 .WithDisabled<UnitSystemControlEnable, UnitDieEnable>()) {
                
                var transform          = transformRef.ValueRO;
                var originalGameObject = unitData.ValueRW.GameObject.Value;
                if (originalGameObject) {
                    originalGameObject.transform.position = transform.Position;
                    originalGameObject.transform.rotation = transform.Rotation;
                }

                // 왼쪽으로 사라져 기본 - Offset
                var offset = -GetRadHalfSize(hitBoxData.ValueRO);
                if (transform.Position.x <= outSidePosX + offset) {
                    if (unitData.ValueRO.GameObject.Value.TryGetComponent<Character.Character>(out var characterScript) && 
                        characterScript.SystemControl.CurrentValue == false &&
                        characterScript.Initialized.CurrentValue) {
                        characterScript.AddState(CharacterState.Die);
                    }
                }
            }
        }

        private float GetRadHalfSize(HitBoxData data)
            => data.Type switch {
                HitBoxType.Circle => data.Radius,
                HitBoxType.Rect   => data.Size.x * 0.5f,
                _                 => 0f
            };
    }
}