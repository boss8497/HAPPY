using Script.GamePlay.ECS.Component;
using Unity.Entities;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CollisionSystem))]
    public partial class CollisionResultSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<EGameTimer>();
        }

        protected override void OnUpdate() {
            var gameTimer   = SystemAPI.GetSingleton<EGameTimer>();
            var delayLookup = SystemAPI.GetBufferLookup<UnitCollisionDelay>();

            foreach (var (unitRef, results) in
                     SystemAPI.Query<RefRO<UnitData>, DynamicBuffer<UnitCollisionResult>>()
                              .WithAll<UnitEntityTag, UnitCollisionResultEnable>()
                              .WithDisabled<UnitSystemControlEnable>()
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
                SystemAPI.SetComponentEnabled<UnitCollisionResultEnable>(unitRef.ValueRO.Entity, false);
            }
        }

        private static int FindDelayIndex(
            DynamicBuffer<UnitCollisionDelay> delays,
            long                              otherUid
        ) {
            for (int i = 0; i < delays.Length; i++) {
                if (delays[i].OtherUid == otherUid) {
                    return i;
                }
            }

            return -1;
        }
    }
}