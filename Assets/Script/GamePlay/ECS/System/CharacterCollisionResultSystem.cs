using Script.GamePlay.ECS.Component;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CharacterCollisionSystem))]
    public partial class CharacterCollisionResultSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<GameTimer>();
        }
        
        protected override void OnUpdate() {
            var gameTimer = SystemAPI.GetSingleton<GameTimer>();
            
            foreach (var (unitRef, results) in
                     SystemAPI.Query<RefRO<UnitData>, DynamicBuffer<UnitCollisionResult>>()
                              .WithAll<UnitEntityTag>()
                              .WithAll<UnitCollisionTag>()
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
                    if (collisionDelay > 0) {
                        SystemAPI.GetBufferLookup<UnitCollisionDelay>()[unitRef.ValueRO.Entity].Add(new UnitCollisionDelay {
                            ExpireTime = gameTimer.Elapsed + collisionDelay,
                            OtherUid   = result.OtherUid
                        });
                    }
                }
                
                results.Clear();
                SystemAPI.SetComponentEnabled<UnitCollisionTag>(unitRef.ValueRO.Entity, false);
            }
        }
    }
}