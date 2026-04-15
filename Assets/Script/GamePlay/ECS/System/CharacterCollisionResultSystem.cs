using Script.GamePlay.ECS.Component;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CharacterCollisionSystem))]
    public partial class CharacterCollisionResultSystem : SystemBase {
        protected override void OnUpdate() {
            foreach (var (unitRef, results) in
                     SystemAPI.Query<RefRO<UnitData>, DynamicBuffer<UnitCollisionResult>>()
                              .WithAll<UnitEntityTag>()
                              ) {
                if (results.Length <= 0)
                    continue;

                var gameObject = unitRef.ValueRO.GameObject.Value;
                if (!gameObject) {
                    results.Clear();
                    continue;
                }

                if (gameObject.TryGetComponent<Character.Character>(out var characterScript) == false) {
                    results.Clear();
                    continue;
                }

                foreach (var result in results) {
                    // Team 체크는 Collision System에서 이미 확인
                    characterScript.Collision(result.OtherUid);
                }

                results.Clear();
            }
        }
    }
}