using Script.GamePlay.ECS.Component;
using Unity.Entities;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class CharacterSyncSystem : SystemBase {
        protected override void OnUpdate() {
            foreach (var (transformRef, effectsRef)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRW<UnitData>>()
                                 .WithAll<UnitEntityTag>()) {
                
                var transform          = transformRef.ValueRO;
                var originalGameObject = effectsRef.ValueRW.GameObject.Value;

                if (originalGameObject) {
                    originalGameObject.transform.position = transform.Position;
                }
            }
        }
    }
}