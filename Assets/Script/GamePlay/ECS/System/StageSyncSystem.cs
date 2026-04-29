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
            
            foreach (var (transformRef, unitData)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRW<UnitData>>()
                                 .WithAll<UnitEntityTag>()) {
                
                var transform          = transformRef.ValueRO;
                var originalGameObject = unitData.ValueRW.GameObject.Value;
                if (originalGameObject) {
                    originalGameObject.transform.position = transform.Position;
                    originalGameObject.transform.rotation = transform.Rotation;
                }

                var offset = -5f;
                if (transform.Position.x <= outSidePosX + offset) {
                    if (unitData.ValueRO.GameObject.Value.TryGetComponent<Character.Character>(out var characterScript)) {
                        characterScript.AddState(CharacterState.Die);
                    }
                }
            }
        }
    }
}