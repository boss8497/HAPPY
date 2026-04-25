using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(RunningSystem))]
    public partial struct CharacterCollisionSystem : ISystem {
        private EntityQuery _collisionQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<EGameTimer>();

            _collisionQuery = SystemAPI.QueryBuilder()
                                       .WithAll<
                                           LocalTransform,
                                           UnitEntityTag,
                                           UnitData,
                                           HitboxActiveShape,
                                           UnitCollisionResult,
                                           UnitCollisionDelay>()
                                       .WithDisabled<UnitDieEnable>()
                                       .WithDisabled<UnitCollisionEnable>()
                                       .WithDisabled<UnitSystemControlEnable>()
                                       .Build();

            state.RequireForUpdate(_collisionQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var gameTimer = SystemAPI.GetSingleton<EGameTimer>();

            var entities   = _collisionQuery.ToEntityArray(Allocator.TempJob);
            var units      = _collisionQuery.ToComponentDataArray<UnitData>(Allocator.TempJob);
            var transforms = _collisionQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            var collisionJob = new CharacterCollisionJob {
                CurrentTime        = gameTimer.Elapsed,
                Entities           = entities,
                Units              = units,
                Transforms         = transforms,
                ActiveLookup       = SystemAPI.GetBufferLookup<HitboxActiveShape>(true),
                ResultLookup       = SystemAPI.GetBufferLookup<UnitCollisionResult>(false),
                DelayLookup        = SystemAPI.GetBufferLookup<UnitCollisionDelay>(false),
                CollisionTagLookup = SystemAPI.GetComponentLookup<UnitCollisionEnable>(false),
            };

            var handle = collisionJob.ScheduleParallelByRef(entities.Length, 32, state.Dependency);

            handle = entities.Dispose(handle);
            handle = units.Dispose(handle);
            handle = transforms.Dispose(handle);

            state.Dependency = handle;
        }

        [BurstCompile]
        public struct CharacterCollisionJob : IJobFor {
            public float CurrentTime;

            [ReadOnly] public NativeArray<Entity>         Entities;
            [ReadOnly] public NativeArray<UnitData>       Units;
            [ReadOnly] public NativeArray<LocalTransform> Transforms;

            [ReadOnly] public BufferLookup<HitboxActiveShape> ActiveLookup;

            [NativeDisableParallelForRestriction]
            public BufferLookup<UnitCollisionResult> ResultLookup;

            [NativeDisableParallelForRestriction]
            public BufferLookup<UnitCollisionDelay> DelayLookup;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<UnitCollisionEnable> CollisionTagLookup;

            public void Execute(int i) {
                var entityA = Entities[i];
                var unitA   = Units[i];

                var shapesA = ActiveLookup[entityA];
                if (shapesA.Length <= 0)
                    return;

                var delayBuffer  = DelayLookup[entityA];
                var resultBuffer = ResultLookup[entityA];

                RemoveExpiredDelay(delayBuffer, CurrentTime);

                var hasCollision = false;

                for (int j = 0; j < Entities.Length; j++) {
                    if (i == j)
                        continue;

                    var unitB = Units[j];

                    // 같은팀 충돌을 피하는건데.. 음
                    // 이거는 캐릭터 끼리의 충돌을 확인하는거고
                    // 만약 Damage가 따로 있다면 Damage System에서 같은팀 판정 옵션을 주는게 좋을듯
                    if (unitA.Team == unitB.Team)
                        continue;

                    if (ContainsDelay(delayBuffer, unitB.Uid))
                        continue;

                    var entityB = Entities[j];
                    var shapesB = ActiveLookup[entityB];
                    if (shapesB.Length <= 0)
                        continue;

                    if (!IntersectsAny(
                            Transforms[i].Position, shapesA,
                            Transforms[j].Position, shapesB)) {
                        continue;
                    }

                    resultBuffer.Add(new UnitCollisionResult {
                        OtherEntity = entityB,
                        OtherUid    = unitB.Uid,
                        OtherTeam   = unitB.Team,
                    });
                    delayBuffer.Add(new UnitCollisionDelay() {
                        OtherUid    = unitB.Uid,
                    });

                    hasCollision = true;
                }

                if (hasCollision) {
                    CollisionTagLookup.SetComponentEnabled(entityA, true);
                }
            }

            private static bool ContainsDelay(
                DynamicBuffer<UnitCollisionDelay> delays,
                long                              otherUid
            ) {
                for (int i = 0; i < delays.Length; i++) {
                    if (delays[i].OtherUid == otherUid)
                        return true;
                }

                return false;
            }

            private static void RemoveExpiredDelay(
                DynamicBuffer<UnitCollisionDelay> delays,
                float                             currentTime
            ) {
                for (int i = delays.Length - 1; i >= 0; i--) {
                    if (delays[i].ExpireTime <= currentTime) {
                        delays.RemoveAtSwapBack(i);
                    }
                }
            }

            private static bool IntersectsAny(
                float3                           unitPosA,
                DynamicBuffer<HitboxActiveShape> shapesA,
                float3                           unitPosB,
                DynamicBuffer<HitboxActiveShape> shapesB
            ) {
                foreach (var a in shapesA) {
                    if (a.Type == HitboxType.None)
                        continue;

                    foreach (var b in shapesB) {
                        if (b.Type == HitboxType.None)
                            continue;

                        if (Intersects(unitPosA, a, unitPosB, b))
                            return true;
                    }
                }

                return false;
            }

            private static bool Intersects(
                float3            unitPosA,
                HitboxActiveShape a,
                float3            unitPosB,
                HitboxActiveShape b
            ) {
                var centerA = unitPosA + a.Offset;
                var centerB = unitPosB + b.Offset;

                if (a.Type == HitboxType.Rect && b.Type == HitboxType.Rect) {
                    return RectVsRect(centerA, a.Size, centerB, b.Size);
                }

                if (a.Type == HitboxType.Circle && b.Type == HitboxType.Circle) {
                    return CircleVsCircle(centerA, a.Radius, centerB, b.Radius);
                }

                if (a.Type == HitboxType.Rect && b.Type == HitboxType.Circle) {
                    return RectVsCircle(centerA, a.Size, centerB, b.Radius);
                }

                if (a.Type == HitboxType.Circle && b.Type == HitboxType.Rect) {
                    return RectVsCircle(centerB, b.Size, centerA, a.Radius);
                }

                return false;
            }

            private static bool RectVsRect(float3 centerA, float3 sizeA, float3 centerB, float3 sizeB) {
                var halfA = sizeA * 0.5f;
                var halfB = sizeB * 0.5f;

                return math.abs(centerA.x - centerB.x) <= (halfA.x + halfB.x) && math.abs(centerA.y - centerB.y) <= (halfA.y + halfB.y);
            }

            private static bool CircleVsCircle(float3 centerA, float radiusA, float3 centerB, float radiusB) {
                var radius = radiusA + radiusB;
                return math.lengthsq(centerA - centerB) <= radius * radius;
            }

            private static bool RectVsCircle(float3 rectCenter, float3 rectSize, float3 circleCenter, float circleRadius) {
                var half = rectSize * 0.5f;
                var min  = rectCenter - half;
                var max  = rectCenter + half;

                var closest = math.clamp(circleCenter, min, max);
                var delta   = circleCenter - closest;

                return math.lengthsq(delta) <= circleRadius * circleRadius;
            }
        }
    }
}