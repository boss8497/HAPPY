using Codice.CM.Common;
using Script.GameInfo.Character;
using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(RunningSystem))]
    public partial struct CollisionSystem : ISystem {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<EGameTimer>();
            _query = SystemAPI.QueryBuilder()
                              .WithAll<UnitData, HitBoxData, LocalTransform, UnitEntityTag>()
                              .WithDisabled<UnitCollisionEnable, UnitDieEnable, UnitSystemControlEnable>()
                              .Build();

            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var gameTimer = SystemAPI.GetSingleton<EGameTimer>();


            var entities   = _query.ToEntityArray(Allocator.TempJob);
            var units      = _query.ToComponentDataArray<UnitData>(Allocator.TempJob);
            var hitBoxes   = _query.ToComponentDataArray<HitBoxData>(Allocator.TempJob);
            var transforms = _query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            var collisionJob = new CollisionJob {
                CurrentTime        = gameTimer.Elapsed,
                Entities           = entities,
                Units              = units,
                HitBoxes           = hitBoxes,
                Transforms         = transforms,
                ResultLookup       = SystemAPI.GetBufferLookup<UnitCollisionResult>(false),
                DelayLookup        = SystemAPI.GetBufferLookup<UnitCollisionDelay>(false),
                CollisionTagLookup = SystemAPI.GetComponentLookup<UnitCollisionEnable>(false),
            };

            var handle = collisionJob.ScheduleParallelByRef(entities.Length, 32, state.Dependency);

            handle = entities.Dispose(handle);
            handle = units.Dispose(handle);
            handle = hitBoxes.Dispose(handle);
            handle = transforms.Dispose(handle);

            state.Dependency = handle;
        }
    }


    [BurstCompile]
    public partial struct CollisionJob : IJobFor {
        public float CurrentTime;

        [ReadOnly] public NativeArray<Entity>         Entities;
        [ReadOnly] public NativeArray<UnitData>       Units;
        [ReadOnly] public NativeArray<LocalTransform> Transforms;
        [ReadOnly] public NativeArray<HitBoxData>     HitBoxes;

        [NativeDisableParallelForRestriction]
        public BufferLookup<UnitCollisionResult> ResultLookup;

        [NativeDisableParallelForRestriction]
        public BufferLookup<UnitCollisionDelay> DelayLookup;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<UnitCollisionEnable> CollisionTagLookup;
        
        public void Execute(int index) {
            var entityA = Entities[index];
            var unitA   = Units[index];
            var hitBox  = HitBoxes[index];
            if (hitBox.Type == HitBoxType.Invisible) return;

            var delayBuffer  = DelayLookup[entityA];
            var resultBuffer = ResultLookup[entityA];

            RemoveExpiredDelay(delayBuffer, CurrentTime);

            var hasCollision = false;

            for (int j = 0; j < Entities.Length; j++) {
                if (index == j)
                    continue;

                var otherHitBox = HitBoxes[index];
                if (otherHitBox.Type == HitBoxType.Invisible) continue;

                var unitB = Units[j];
                // 같은팀 충돌을 피하는건데.. 음
                // 이거는 캐릭터 끼리의 충돌을 확인하는거고
                // 만약 Damage가 따로 있다면 Damage System에서 같은팀 판정 옵션을 주는게 좋을듯
                if (unitA.Team == unitB.Team)
                    continue;

                if (ContainsDelay(delayBuffer, unitB.Uid))
                    continue;

                var entityB = Entities[j];

                if (!CollisionCheck(
                        Transforms[index].Position, hitBox,
                        Transforms[j].Position, otherHitBox)) {
                    continue;
                }

                resultBuffer.Add(new UnitCollisionResult {
                    OtherEntity = entityB,
                    OtherUid    = unitB.Uid,
                    OtherTeam   = unitB.Team,
                });

                delayBuffer.Add(new UnitCollisionDelay() {
                    OtherUid = unitB.Uid,
                });

                hasCollision = true;
            }

            if (hasCollision) {
                CollisionTagLookup.SetComponentEnabled(entityA, true);
            }
        }

        // Collision Delay 제거
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

        // Collision Delay 확인
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

        // Hit
        private static bool CollisionCheck(
            float3     unitPosA,
            HitBoxData a,
            float3     unitPosB,
            HitBoxData b
        ) {
            var centerA = unitPosA + a.Offset;
            var centerB = unitPosB + b.Offset;

            if (a.Type == HitBoxType.Rect && b.Type == HitBoxType.Rect) {
                return RectVsRect(centerA, a.Size, centerB, b.Size);
            }

            if (a.Type == HitBoxType.Circle && b.Type == HitBoxType.Circle) {
                return CircleVsCircle(centerA, a.Radius, centerB, b.Radius);
            }

            if (a.Type == HitBoxType.Rect && b.Type == HitBoxType.Circle) {
                return RectVsCircle(centerA, a.Size, centerB, b.Radius);
            }

            if (a.Type == HitBoxType.Circle && b.Type == HitBoxType.Rect) {
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