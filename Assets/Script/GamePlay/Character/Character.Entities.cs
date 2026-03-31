using Script.GameInfo.Character;
using Script.GamePlay.ECS.Component;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.GamePlay.Character {
    public partial class Character {
        private void InitializeCharacterHitboxEntity() {
            if (_unitManager == null)
                return;

            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;

            EnsureHitboxComponents(entityManager, entity);

            var presetRanges = entityManager.GetBuffer<CharacterHitboxPresetRangeData>(entity);
            var presetShapes = entityManager.GetBuffer<CharacterHitboxPresetShapeData>(entity);
            var activeShapes = entityManager.GetBuffer<CharacterHitboxActiveShapeData>(entity);

            presetRanges.Clear();
            presetShapes.Clear();
            activeShapes.Clear();

            var characterInfo = CharacterCharacterInfo;
            if (characterInfo == null) {
                entityManager.SetComponentData(entity, new CharacterHitboxStateData {
                    Current = State,
                    Applied = CharacterState.None,
                });
                return;
            }

            RegisterDefaultHitbox(characterInfo, presetRanges, presetShapes);
            RegisterStateHitboxes(characterInfo, presetRanges, presetShapes);

            var appliedState = ApplyActiveHitboxes(State, presetRanges, presetShapes, activeShapes);

            entityManager.SetComponentData(entity, new CharacterHitboxStateData {
                Current = State,
                Applied = appliedState,
            });
        }

        private void SyncCharacterHitboxEntity() {
            if (_unitManager == null)
                return;

            _unitManager.SyncUnitEntity(this);

            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;

            if (entityManager.HasComponent<CharacterHitboxStateData>(entity) == false) {
                InitializeCharacterHitboxEntity();
                return;
            }

            var presetRanges = entityManager.GetBuffer<CharacterHitboxPresetRangeData>(entity);
            var presetShapes = entityManager.GetBuffer<CharacterHitboxPresetShapeData>(entity);
            var activeShapes = entityManager.GetBuffer<CharacterHitboxActiveShapeData>(entity);

            if (presetRanges.Length <= 0 || presetShapes.Length <= 0) {
                InitializeCharacterHitboxEntity();
                return;
            }

            var appliedState = ApplyActiveHitboxes(State, presetRanges, presetShapes, activeShapes);

            entityManager.SetComponentData(entity, new CharacterHitboxStateData {
                Current = State,
                Applied = appliedState,
            });
        }

        private static void EnsureHitboxComponents(EntityManager entityManager, Entity entity) {
            if (entityManager.HasComponent<CharacterHitboxStateData>(entity) == false) {
                entityManager.AddComponentData(entity, new CharacterHitboxStateData {
                    Current = CharacterState.None,
                    Applied = CharacterState.None,
                });
            }

            if (entityManager.HasBuffer<CharacterHitboxPresetRangeData>(entity) == false) {
                entityManager.AddBuffer<CharacterHitboxPresetRangeData>(entity);
            }

            if (entityManager.HasBuffer<CharacterHitboxPresetShapeData>(entity) == false) {
                entityManager.AddBuffer<CharacterHitboxPresetShapeData>(entity);
            }

            if (entityManager.HasBuffer<CharacterHitboxActiveShapeData>(entity) == false) {
                entityManager.AddBuffer<CharacterHitboxActiveShapeData>(entity);
            }
        }

        private static void RegisterDefaultHitbox(
            CharacterInfo characterInfo,
            DynamicBuffer<CharacterHitboxPresetRangeData> presetRanges,
            DynamicBuffer<CharacterHitboxPresetShapeData> presetShapes) {

            if (characterInfo.hitbox == null)
                return;

            AddPresetRange(CharacterState.None, characterInfo.hitbox, presetRanges, presetShapes);
        }

        private static void RegisterStateHitboxes(
            CharacterInfo                                 characterInfo,
            DynamicBuffer<CharacterHitboxPresetRangeData> presetRanges,
            DynamicBuffer<CharacterHitboxPresetShapeData> presetShapes) {
            if (characterInfo.hitboxes == null || characterInfo.hitboxes.Length <= 0)
                return;

            foreach (var stateHitbox in characterInfo.hitboxes) {
                if (stateHitbox == null)
                    continue;

                AddPresetRange(stateHitbox.state, stateHitbox.hitboxes, presetRanges, presetShapes);
            }
        }

        private static void AddPresetRange(
            CharacterState stateMask,
            Hitbox source,
            DynamicBuffer<CharacterHitboxPresetRangeData> presetRanges,
            DynamicBuffer<CharacterHitboxPresetShapeData> presetShapes) {

            if (source == null)
                return;

            if (TryConvertHitbox(source, out var shape) == false)
                return;

            int startIndex = presetShapes.Length;
            presetShapes.Add(shape);

            presetRanges.Add(new CharacterHitboxPresetRangeData {
                StateMask  = stateMask,
                StartIndex = startIndex,
                Length     = 1,
            });
        }

        private static void AddPresetRange(
            CharacterState stateMask,
            Hitbox[] sources,
            DynamicBuffer<CharacterHitboxPresetRangeData> presetRanges,
            DynamicBuffer<CharacterHitboxPresetShapeData> presetShapes) {

            if (sources == null || sources.Length <= 0)
                return;

            int startIndex = presetShapes.Length;
            int length = 0;

            foreach (var t in sources) {
                if (TryConvertHitbox(t, out var shape) == false)
                    continue;

                presetShapes.Add(shape);
                length++;
            }

            if (length <= 0)
                return;

            presetRanges.Add(new CharacterHitboxPresetRangeData {
                StateMask  = stateMask,
                StartIndex = startIndex,
                Length     = length,
            });
        }

        private static bool TryConvertHitbox(Hitbox source, out CharacterHitboxPresetShapeData shape) {
            switch (source) {
                case RectHitbox rect:
                    shape = new CharacterHitboxPresetShapeData {
                        ShapeType = CharacterHitboxShapeType.Rect,
                        Offset    = new float2(rect.offset.x, rect.offset.y),
                        Size      = new float2(rect.size.x, rect.size.y),
                        Radius    = 0f,
                    };
                    return true;

                case CircleHitbox circle:
                    shape = new CharacterHitboxPresetShapeData {
                        ShapeType = CharacterHitboxShapeType.Circle,
                        Offset    = new float2(circle.offset.x, circle.offset.y),
                        Size      = float2.zero,
                        Radius    = circle.radius,
                    };
                    return true;

                default:
                    shape = default;
                    return false;
            }
        }

        private static CharacterState ApplyActiveHitboxes(
            CharacterState currentState,
            DynamicBuffer<CharacterHitboxPresetRangeData> presetRanges,
            DynamicBuffer<CharacterHitboxPresetShapeData> presetShapes,
            DynamicBuffer<CharacterHitboxActiveShapeData> activeShapes) {

            activeShapes.Clear();

            int rangeIndex = FindBestRangeIndex(currentState, presetRanges);
            if (rangeIndex < 0)
                return CharacterState.None;

            var range = presetRanges[rangeIndex];

            for (int i = 0; i < range.Length; i++) {
                var src = presetShapes[range.StartIndex + i];

                activeShapes.Add(new CharacterHitboxActiveShapeData {
                    ShapeType = src.ShapeType,
                    Offset    = src.Offset,
                    Size      = src.Size,
                    Radius    = src.Radius,
                });
            }

            return range.StateMask;
        }

        /// <summary>
        /// hitboxes 배열 순서대로 먼저 매칭된 상태를 사용하고,
        /// 없으면 CharacterInfo.hitbox (StateMask == None) 를 기본값으로 사용
        /// </summary>
        private static int FindBestRangeIndex(
            CharacterState currentState,
            DynamicBuffer<CharacterHitboxPresetRangeData> presetRanges) {

            int defaultIndex = -1;

            for (int i = 0; i < presetRanges.Length; i++) {
                var range = presetRanges[i];

                if (range.StateMask == CharacterState.None) {
                    if (defaultIndex < 0) {
                        defaultIndex = i;
                    }
                    continue;
                }

                if ((currentState & range.StateMask) == range.StateMask) {
                    return i;
                }
            }

            return defaultIndex;
        }
    }
}