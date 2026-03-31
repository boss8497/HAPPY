using Script.GamePlay.ECS.Component;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Script.GamePlay.ECS.System {
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CharacterCollisionSystem : ISystem {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state) {
            _query = SystemAPI.QueryBuilder()
                              .WithAll<
                                  UnitEntityTag,
                                  UnitIdentityData,
                                  UnitTransformData,
                                  CharacterHitboxActiveShapeData,
                                  CharacterCollisionResultData>()
                              .Build();

            state.RequireForUpdate(_query);
        }

        public void OnUpdate(ref SystemState state) {
            foreach (var resultBuffer in SystemAPI.Query<DynamicBuffer<CharacterCollisionResultData>>()) {
                resultBuffer.Clear();
            }

            var entities   = _query.ToEntityArray(Allocator.Temp);
            var identities = _query.ToComponentDataArray<UnitIdentityData>(Allocator.Temp);
            var transforms = _query.ToComponentDataArray<UnitTransformData>(Allocator.Temp);

            var activeLookup = SystemAPI.GetBufferLookup<CharacterHitboxActiveShapeData>(true);
            var resultLookup = SystemAPI.GetBufferLookup<CharacterCollisionResultData>(false);

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
                        .Add(new CharacterCollisionResultData {
                            OtherEntity = entityB,
                            OtherUid    = identities[j].Uid,
                            OtherTeam   = identities[j].Team,
                        });

                    resultLookup[entityB]
                        .Add(new CharacterCollisionResultData {
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
            float2                                        unitPosA,
            DynamicBuffer<CharacterHitboxActiveShapeData> shapesA,
            float2                                        unitPosB,
            DynamicBuffer<CharacterHitboxActiveShapeData> shapesB
        ) {
            
            foreach (var a in shapesA) {
                if (a.ShapeType == CharacterHitboxShapeType.None)
                    continue;

                foreach (var b in shapesB) {
                    if (b.ShapeType == CharacterHitboxShapeType.None)
                        continue;

                    if (Intersects(unitPosA, a, unitPosB, b)) {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Intersects(
            float2                         unitPosA,
            CharacterHitboxActiveShapeData a,
            float2                         unitPosB,
            CharacterHitboxActiveShapeData b
        ) {
            float2 centerA = unitPosA + a.Offset;
            float2 centerB = unitPosB + b.Offset;

            if (a.ShapeType == CharacterHitboxShapeType.Rect && b.ShapeType == CharacterHitboxShapeType.Rect) {
                return RectVsRect(centerA, a.Size, centerB, b.Size);
            }

            if (a.ShapeType == CharacterHitboxShapeType.Circle && b.ShapeType == CharacterHitboxShapeType.Circle) {
                return CircleVsCircle(centerA, a.Radius, centerB, b.Radius);
            }

            if (a.ShapeType == CharacterHitboxShapeType.Rect && b.ShapeType == CharacterHitboxShapeType.Circle) {
                return RectVsCircle(centerA, a.Size, centerB, b.Radius);
            }

            if (a.ShapeType == CharacterHitboxShapeType.Circle && b.ShapeType == CharacterHitboxShapeType.Rect) {
                return RectVsCircle(centerB, b.Size, centerA, a.Radius);
            }

            return false;
        }

        private static bool RectVsRect(float2 centerA, float2 sizeA, float2 centerB, float2 sizeB) {
            float2 halfA = sizeA * 0.5f;
            float2 halfB = sizeB * 0.5f;

            return math.abs(centerA.x - centerB.x) <= (halfA.x + halfB.x) && math.abs(centerA.y - centerB.y) <= (halfA.y + halfB.y);
        }

        private static bool CircleVsCircle(float2 centerA, float radiusA, float2 centerB, float radiusB) {
            float radius = radiusA + radiusB;
            return math.lengthsq(centerA - centerB) <= radius * radius;
        }

        private static bool RectVsCircle(float2 rectCenter, float2 rectSize, float2 circleCenter, float circleRadius) {
            float2 half = rectSize * 0.5f;
            float2 min  = rectCenter - half;
            float2 max  = rectCenter + half;

            float2 closest = math.clamp(circleCenter, min, max);
            float2 delta   = circleCenter - closest;

            return math.lengthsq(delta) <= circleRadius * circleRadius;
        }
    }
}