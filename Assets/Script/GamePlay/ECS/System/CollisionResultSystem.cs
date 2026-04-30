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
                              .WithAll<UnitEntityTag, UnitCollisionEnable>()
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
                    var collisionDelay = characterScript.GetCollisionDelayTime();
                    var delayBuffer    = delayLookup[unitRef.ValueRO.Entity];
                    var index          = FindDelayIndex(delayBuffer, result.OtherUid);

                    // CollisionSystem에서 버퍼 등록을 하는데 없으면 문제가 있음..
                    // 새로 만들어 준다?
                    if (index <= 0) {
                        delayBuffer.Add(new UnitCollisionDelay {
                                            ExpireTime = gameTimer.Elapsed + collisionDelay,
                                            OtherUid   = result.OtherUid
                                        });
                    }
                    else {
                        var oldDelayBuffer = delayBuffer[index];
                        oldDelayBuffer.ExpireTime = gameTimer.Elapsed + collisionDelay;
                        delayBuffer[index]        = oldDelayBuffer;
                    }
                }

                results.Clear();
                SystemAPI.SetComponentEnabled<UnitCollisionEnable>(unitRef.ValueRO.Entity, false);
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