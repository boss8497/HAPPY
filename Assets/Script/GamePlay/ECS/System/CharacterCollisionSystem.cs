using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(RunningSystem))]
    public partial struct CharacterCollisionSystem : ISystem {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            _query = SystemAPI.QueryBuilder()
                              .WithAll<
                                  LocalTransform,
                                  UnitEntityTag,
                                  UnitData,
                                  HitboxActiveShape,
                                  CollisionResultData>()
                              .Build();

            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            foreach (var resultBuffer in SystemAPI.Query<DynamicBuffer<CollisionResultData>>()) {
                resultBuffer.Clear();
            }

            var entities   = _query.ToEntityArray(Allocator.Temp);
            var identities = _query.ToComponentDataArray<UnitData>(Allocator.Temp);
            var transforms = _query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            var activeLookup = SystemAPI.GetBufferLookup<HitboxActiveShape>(true);
            var resultLookup = SystemAPI.GetBufferLookup<CollisionResultData>(false);

            for (int i = 0; i < entities.Length; i++) {
                var entityA = entities[i];
                var shapesA = activeLookup[entityA];
                if (shapesA.Length <= 0)
                    continue;

                for (int j = i + 1; j < entities.Length; j++) {
                    var entityB = entities[j];
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
                        .Add(new CollisionResultData {
                            OtherEntity = entityB,
                            OtherUid    = identities[j].Uid,
                            OtherTeam   = identities[j].Team,
                        });

                    resultLookup[entityB]
                        .Add(new CollisionResultData {
                            OtherEntity = entityA,
                            OtherUid    = identities[i].Uid,
                            OtherTeam   = identities[i].Team,
                        });
                }
            }

            entities.Dispose();
            identities.Dispose();
            transforms.Dispose();
        }

        private static bool IntersectsAny(
            float3                                        unitPosA,
            DynamicBuffer<HitboxActiveShape> shapesA,
            float3                                        unitPosB,
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
            float3                         unitPosA,
            HitboxActiveShape a,
            float3                         unitPosB,
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