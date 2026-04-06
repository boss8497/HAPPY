using Script.GamePlay.ECS.Component;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CharacterCollisionSystem))]
    public partial class CharacterCollisionResultSystem : SystemBase {
        private EntityQuery _query;

        protected override void OnCreate() {
            _query = SystemAPI.QueryBuilder()
                              .WithAll<
                                  LocalTransform,
                                  UnitData,
                                  CollisionResultData>()
                              .Build();
            RequireForUpdate(_query);
        }

        protected override void OnUpdate() {
            foreach (var (unitRef, results) in
                     SystemAPI.Query<RefRO<UnitData>, DynamicBuffer<CollisionResultData>>()
                              .WithAll<UnitEntityTag>()) {
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
                    // 필요하면 여기서 팀 체크 / UID 체크 / 데미지 분기 가능
                    characterScript.ApplyDamage();
                }

                results.Clear();
            }
        }
    }
}