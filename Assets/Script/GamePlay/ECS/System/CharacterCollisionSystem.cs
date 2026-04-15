using System.Linq;
using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(RunningSystem))]
    public partial struct CharacterCollisionSystem : ISystem {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<GameTimer>();
            
            _query = SystemAPI.QueryBuilder()
                              .WithAll<
                                  LocalTransform,
                                  UnitEntityTag,
                                  UnitData,
                                  HitboxActiveShape,
                                  UnitCollisionResult,
                                  UnitCollisionDelay>()
                              .WithDisabled<UnitDieTag>()
                              .WithDisabled<UnitCollisionTag>()
                              .Build();

            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            foreach (var resultBuffer in SystemAPI.Query<DynamicBuffer<UnitCollisionResult>>()) {
                resultBuffer.Clear();
            }
            
            var gameTimer = SystemAPI.GetSingleton<GameTimer>();

            var entities   = _query.ToEntityArray(Allocator.Temp);
            var identities = _query.ToComponentDataArray<UnitData>(Allocator.Temp);
            var transforms = _query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            var activeLookup = SystemAPI.GetBufferLookup<HitboxActiveShape>(true);
            var resultLookup = SystemAPI.GetBufferLookup<UnitCollisionResult>(false);
            var delayLookup  = SystemAPI.GetBufferLookup<UnitCollisionDelay>(false);

            for (int i = 0; i < entities.Length; i++) {
                var entityA = entities[i];
                var shapesA = activeLookup[entityA];
                if (shapesA.Length <= 0)
                    continue;

                var unitA = identities[i];
                var delayBuffer = delayLookup[entityA];
                RemoveExpiredDelay(delayBuffer, gameTimer.Elapsed);

                for (int j = 0; j < entities.Length; j++) {
                    if (i == j) continue;

                    if (ContainsDelay(delayBuffer, identities[j].Uid)) {
                        Debug.LogError($"Collision between {unitA.Uid} and {identities[j].Uid} is delayed.");
                        continue;
                    }

                    var unitB   = identities[j];
                    var entityB = entities[j];

                    // 같은팀 충돌을 피하는건데.. 음
                    // 이거는 캐릭터 끼리의 충돌을 확인하는거고
                    // 만약 Damage가 따로 있다면 Damage System에서 같은팀 판정 옵션을 주는게 좋을듯
                    if (unitA.Team == unitB.Team) continue;

                    var shapesB = activeLookup[entityB];
                    if (shapesB.Length <= 0)
                        continue;

                    if (IntersectsAny(
                            transforms[i].Position, shapesA,
                            transforms[j].Position, shapesB)
                     == false) {
                        continue;
                    }


                    resultLookup[entityA]
                        .Add(new UnitCollisionResult {
                            OtherEntity = entityB,
                            OtherUid    = identities[j].Uid,
                            OtherTeam   = identities[j].Team,
                        });
                    
                    SystemAPI.SetComponentEnabled<UnitCollisionTag>(entityA, true);
                }
            }

            entities.Dispose();
            identities.Dispose();
            transforms.Dispose();
        }

        private bool ContainsDelay(
            DynamicBuffer<UnitCollisionDelay> delays,
            long                              otherUid
        ) {
            for (int i = 0; i < delays.Length; i++) {
                if (delays[i].OtherUid == otherUid) {
                    return true;
                }
            }

            return false;
        }
        private void RemoveExpiredDelay(
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

                    if (Intersects(unitPosA, a, unitPosB, b)) {
                        return true;
                    }
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
            float3 centerA = unitPosA + a.Offset;
            float3 centerB = unitPosB + b.Offset;

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
            float3 halfA = sizeA * 0.5f;
            float3 halfB = sizeB * 0.5f;

            return math.abs(centerA.x - centerB.x) <= (halfA.x + halfB.x) && math.abs(centerA.y - centerB.y) <= (halfA.y + halfB.y);
        }

        private static bool CircleVsCircle(float3 centerA, float radiusA, float3 centerB, float radiusB) {
            float radius = radiusA + radiusB;
            return math.lengthsq(centerA - centerB) <= radius * radius;
        }

        private static bool RectVsCircle(float3 rectCenter, float3 rectSize, float3 circleCenter, float circleRadius) {
            float3 half = rectSize * 0.5f;
            float3 min  = rectCenter - half;
            float3 max  = rectCenter + half;

            float3 closest = math.clamp(circleCenter, min, max);
            float3 delta   = circleCenter - closest;

            return math.lengthsq(delta) <= circleRadius * circleRadius;
        }
    }
}