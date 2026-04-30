using Codice.CM.Common;
using Script.GameInfo.Character;
using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
                              .WithAll<UnitData, HitBoxData, LocalTransform, UnitEntityTag, UnitCollisionEnable>()
                              .WithDisabled<UnitCollisionResultEnable, UnitDieEnable, UnitSystemControlEnable>()
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
                CollisionTagLookup = SystemAPI.GetComponentLookup<UnitCollisionResultEnable>(false),
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
        public ComponentLookup<UnitCollisionResultEnable> CollisionTagLookup;

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
                        Transforms[index], hitBox,
                        Transforms[j], otherHitBox)) {
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
            LocalTransform my,
            HitBoxData     myData,
            LocalTransform target,
            HitBoxData     targetData
        ) {
            if (myData.Type == HitBoxType.Rect && targetData.Type == HitBoxType.Rect) {
                return RectVsRect(my, myData, target, targetData);
            }

            if (myData.Type == HitBoxType.Circle && targetData.Type == HitBoxType.Circle) {
                return CircleVsCircle(my, myData, target, targetData);
            }

            if (myData.Type == HitBoxType.Rect && targetData.Type == HitBoxType.Circle) {
                return RectVsCircle(my, myData, target, targetData);
            }

            if (myData.Type == HitBoxType.Circle && targetData.Type == HitBoxType.Rect) {
                return RectVsCircle(my, myData, target, targetData);
            }

            return false;
        }

        // 3D 계산이 아니라 2D(XY) 계산
        private static bool RectVsRect(
            LocalTransform my,
            HitBoxData     myData,
            LocalTransform target,
            HitBoxData     targetData
        ) {
            var myCenter     = my.Position + math.rotate(my.Rotation, myData.Offset);
            var targetCenter = target.Position + math.rotate(target.Rotation, targetData.Offset);

            var halfA = myData.Size * 0.5f;
            var halfB = targetData.Size * 0.5f;

            // my Rect의 로컬 축을 월드 방향으로 변환
            var aRight = math.rotate(my.Rotation, new float3(1f, 0f, 0f));
            var aUp    = math.rotate(my.Rotation, new float3(0f, 1f, 0f));

            // target Rect의 로컬 축을 월드 방향으로 변환
            var bRight = math.rotate(target.Rotation, new float3(1f, 0f, 0f));
            var bUp    = math.rotate(target.Rotation, new float3(0f, 1f, 0f));

            var centerDelta = targetCenter - myCenter;

            // SAT 검사 축:
            // A의 Right, A의 Up, B의 Right, B의 Up
            if (!OverlapOnAxis(centerDelta, aRight, aRight, aUp, halfA, bRight, bUp, halfB))
                return false;

            if (!OverlapOnAxis(centerDelta, aUp, aRight, aUp, halfA, bRight, bUp, halfB))
                return false;

            if (!OverlapOnAxis(centerDelta, bRight, aRight, aUp, halfA, bRight, bUp, halfB))
                return false;

            if (!OverlapOnAxis(centerDelta, bUp, aRight, aUp, halfA, bRight, bUp, halfB))
                return false;

            return true;
        }

        private static bool OverlapOnAxis(
            float3 centerDelta,
            float3 axis,
            float3 aRight,
            float3 aUp,
            float3 halfA,
            float3 bRight,
            float3 bUp,
            float3 halfB
        ) {
            var axisLengthSq = math.lengthsq(axis);

            if (axisLengthSq < float.Epsilon)
                return true;

            axis *= math.rsqrt(axisLengthSq);

            // 두 중심 사이 거리를 검사 축에 투영
            var distance = math.abs(math.dot(centerDelta, axis));

            // A Rect가 해당 축 위에서 차지하는 반지름
            var radiusA =
                halfA.x * math.abs(math.dot(aRight, axis)) + halfA.y * math.abs(math.dot(aUp, axis));

            // B Rect가 해당 축 위에서 차지하는 반지름
            var radiusB =
                halfB.x * math.abs(math.dot(bRight, axis)) + halfB.y * math.abs(math.dot(bUp, axis));

            return distance <= radiusA + radiusB;
        }

        private static bool CircleVsCircle(LocalTransform my, HitBoxData myData, LocalTransform target, HitBoxData targetData) {
            var myCenter     = my.Position + myData.Offset;
            var targetCenter = target.Position + targetData.Offset;

            var radius = myData.Radius + targetData.Radius;
            return math.lengthsq(myCenter - targetCenter) <= radius * radius;
        }

        private static bool RectVsCircle(LocalTransform my, HitBoxData myData, LocalTransform target, HitBoxData targetData) {
            // 1. HitBox Offset이 로컬 좌표 기준이라면,
            //    회전값을 적용해서 월드 좌표의 중심을 구해야 함.
            var myCenter = my.Position + math.rotate(my.Rotation, myData.Offset);

            var targetCenter = target.Position + math.rotate(target.Rotation, targetData.Offset);

            // 2. 내 사각형/박스의 절반 크기
            var half = myData.Size * 0.5f;

            // 3. targetCenter를 my 사각형 기준의 로컬 좌표로 변환
            //    즉, myCenter를 원점으로 보고,
            //    my.Rotation의 반대 회전을 적용한다.
            var localTargetCenter = math.rotate(
                math.inverse(my.Rotation),
                targetCenter - myCenter
            );

            // 4. 로컬 공간에서는 회전이 풀린 AABB처럼 볼 수 있음
            //    범위는 -half ~ +half
            var closest = math.clamp(
                localTargetCenter,
                -half,
                half
            );

            // 5. 로컬 공간에서 원/구 중심과 박스의 최근접 점 거리 비교
            var delta = localTargetCenter - closest;

            return math.lengthsq(delta) <= targetData.Radius * targetData.Radius;
        }
    }
}