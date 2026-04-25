using Script.GameInfo.Character;
using Script.GamePlay.ECS.Component;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.GamePlay.Character {
    public partial class Character {
        private void InitializeEntity() {
            if (_unitManager == null)
                return;

            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;

            EnsureComponents(entityManager, entity);

            var presetRanges = entityManager.GetBuffer<HitboxPresetRange>(entity);
            var presetShapes = entityManager.GetBuffer<HitboxPresetShape>(entity);
            var activeShapes = entityManager.GetBuffer<HitboxActiveShape>(entity);

            presetRanges.Clear();
            presetShapes.Clear();
            activeShapes.Clear();

            var characterInfo = CharacterInfo;
            if (characterInfo == null) {
                entityManager.SetComponentData(entity, new HitboxState {
                    Current = State.CurrentValue,
                    Applied = CharacterState.None,
                });
                return;
            }

            RegisterDefaultHitbox(characterInfo, presetRanges, presetShapes);
            RegisterStateHitboxes(characterInfo, presetRanges, presetShapes);

            var appliedState = ApplyActiveHitboxes(State.CurrentValue, presetRanges, presetShapes, activeShapes);

            entityManager.SetComponentData(entity, new HitboxState {
                Current = State.CurrentValue,
                Applied = appliedState,
            });
        }

        private void SyncCharacterHitboxEntity() {
            if (_unitManager == null)
                return;

            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;

            if (entityManager.HasComponent<HitboxState>(entity) == false) {
                InitializeEntity();
                return;
            }

            var presetRanges = entityManager.GetBuffer<HitboxPresetRange>(entity);
            var presetShapes = entityManager.GetBuffer<HitboxPresetShape>(entity);
            var activeShapes = entityManager.GetBuffer<HitboxActiveShape>(entity);

            if (presetRanges.Length <= 0 || presetShapes.Length <= 0) {
                InitializeEntity();
                return;
            }

            var appliedState = ApplyActiveHitboxes(State.CurrentValue, presetRanges, presetShapes, activeShapes);

            entityManager.SetComponentData(entity, new HitboxState {
                Current = State.CurrentValue,
                Applied = appliedState,
            });
        }

        private void EnsureComponents(EntityManager entityManager, Entity entity) {
            if (entityManager.HasComponent<UnitDieEnable>(entity) == false) {
                entityManager.AddComponentData<UnitDieEnable>(entity, new());
            }
            SetEnabledTag<UnitDieEnable>(false);
            
            if (entityManager.HasComponent<UnitRunningEnable>(entity) == false) {
                entityManager.AddComponentData<UnitRunningEnable>(entity, new());
            }
            SetEnabledTag<UnitRunningEnable>(false);
            
            if (entityManager.HasComponent<UnitCollisionEnable>(entity) == false) {
                entityManager.AddComponentData<UnitCollisionEnable>(entity, new());
            }
            SetEnabledTag<UnitCollisionEnable>(false);
            
            if (entityManager.HasComponent<UnitSystemControlEnable>(entity) == false) {
                entityManager.AddComponentData<UnitSystemControlEnable>(entity, new());
            }
            SetEnabledTag<UnitSystemControlEnable>(SystemControl?.CurrentValue ?? false);
            
            if (entityManager.HasComponent<HitboxState>(entity) == false) {
                entityManager.AddComponentData(entity, new HitboxState {
                    Current = CharacterState.None,
                    Applied = CharacterState.None,
                });
            }

            if (entityManager.HasBuffer<HitboxPresetRange>(entity) == false) {
                entityManager.AddBuffer<HitboxPresetRange>(entity);
            }

            if (entityManager.HasBuffer<HitboxPresetShape>(entity) == false) {
                entityManager.AddBuffer<HitboxPresetShape>(entity);
            }

            if (entityManager.HasBuffer<HitboxActiveShape>(entity) == false) {
                entityManager.AddBuffer<HitboxActiveShape>(entity);
            }

            if (entityManager.HasBuffer<UnitCollisionResult>(entity) == false) {
                entityManager.AddBuffer<UnitCollisionResult>(entity);
            }
            
            if (entityManager.HasBuffer<UnitCollisionDelay>(entity) == false) {
                entityManager.AddBuffer<UnitCollisionDelay>(entity);
            }

            
            if (IsPlayer) {
                if (entityManager.HasComponent<RunningData>(entity) == false) {
                    entityManager.AddComponentData(entity, new RunningData {
                        Direction = new float3(1f, 0f, 0f),
                        Speed     = 0f,
                    });
                }   
                
                
                if (entityManager.HasComponent<JumpInputData>(entity) == false) {
                    entityManager.AddComponentData(entity, new JumpInputData {
                        Held             = 0,
                        ReleaseRequested = 0,
                    });
                }

                if (entityManager.HasComponent<JumpResultData>(entity) == false) {
                    entityManager.AddComponentData(entity, new JumpResultData {
                        Landed = 0,
                    });
                }

                if (entityManager.HasComponent<JumpingData>(entity) == false) {
                    entityManager.AddComponentData(entity, new JumpingData {
                        GroundY         = 0f,
                        CurrentJumpTime = 0f,
                        MaxJumpTime     = 0f,
                        Gravity         = 0f,
                        FallGravity     = 0f,
                        Timer           = 0f,
                        JumpVelocity    = 0f,
                    });

                    entityManager.SetComponentEnabled<JumpingData>(entity, false);
                }
            }
        }
        
        private void SetEnabledTag<T>(bool enable) where T : IComponentData, IEnableableComponent {
            if (_unitManager.TryGetEntity(this, out var entity) == false)
                return;

            var entityManager = _stageEntityWorld.EntityManager;
            if (entityManager.HasComponent<T>(entity) == false)
                return;

            entityManager.SetComponentEnabled<T>(entity, enable);
        }

        private static void RegisterDefaultHitbox(
            CharacterInfo                                 characterInfo,
            DynamicBuffer<HitboxPresetRange> presetRanges,
            DynamicBuffer<HitboxPresetShape> presetShapes
        ) {
            if (characterInfo.hitbox == null)
                return;

            AddPresetRange(CharacterState.None, characterInfo.hitbox, presetRanges, presetShapes);
        }

        private static void RegisterStateHitboxes(
            CharacterInfo                                 characterInfo,
            DynamicBuffer<HitboxPresetRange> presetRanges,
            DynamicBuffer<HitboxPresetShape> presetShapes
        ) {
            if (characterInfo.hitboxes == null || characterInfo.hitboxes.Length <= 0)
                return;

            foreach (var stateHitbox in characterInfo.hitboxes) {
                if (stateHitbox == null)
                    continue;

                AddPresetRange(stateHitbox.state, stateHitbox.hitboxes, presetRanges, presetShapes);
            }
        }

        private static void AddPresetRange(
            CharacterState                                stateMask,
            Hitbox                                        source,
            DynamicBuffer<HitboxPresetRange> presetRanges,
            DynamicBuffer<HitboxPresetShape> presetShapes
        ) {
            if (source == null)
                return;

            if (TryConvertHitbox(source, out var shape) == false)
                return;

            int startIndex = presetShapes.Length;
            presetShapes.Add(shape);

            presetRanges.Add(new HitboxPresetRange {
                StateMask  = stateMask,
                StartIndex = startIndex,
                Length     = 1,
            });
        }

        private static void AddPresetRange(
            CharacterState                                stateMask,
            Hitbox[]                                      sources,
            DynamicBuffer<HitboxPresetRange> presetRanges,
            DynamicBuffer<HitboxPresetShape> presetShapes
        ) {
            if (sources == null || sources.Length <= 0)
                return;

            int startIndex = presetShapes.Length;
            int length     = 0;

            foreach (var t in sources) {
                if (TryConvertHitbox(t, out var shape) == false)
                    continue;

                presetShapes.Add(shape);
                length++;
            }

            if (length <= 0)
                return;

            presetRanges.Add(new HitboxPresetRange {
                StateMask  = stateMask,
                StartIndex = startIndex,
                Length     = length,
            });
        }

        private static bool TryConvertHitbox(Hitbox source, out HitboxPresetShape shape) {
            switch (source) {
                case RectHitbox rect:
                    shape = new HitboxPresetShape {
                        Type = HitboxType.Rect,
                        Offset    = rect.offset,
                        Size      = rect.size,
                        Radius    = 0f,
                    };
                    return true;

                case CircleHitbox circle:
                    shape = new HitboxPresetShape {
                        Type = HitboxType.Circle,
                        Offset    = circle.offset,
                        Size      = float3.zero,
                        Radius    = circle.radius,
                    };
                    return true;

                default:
                    shape = default;
                    return false;
            }
        }

        private static CharacterState ApplyActiveHitboxes(
            CharacterState                                currentState,
            DynamicBuffer<HitboxPresetRange> presetRanges,
            DynamicBuffer<HitboxPresetShape> presetShapes,
            DynamicBuffer<HitboxActiveShape> activeShapes
        ) {
            activeShapes.Clear();

            int rangeIndex = FindBestRangeIndex(currentState, presetRanges);
            if (rangeIndex < 0)
                return CharacterState.None;

            var range = presetRanges[rangeIndex];

            for (int i = 0; i < range.Length; i++) {
                var src = presetShapes[range.StartIndex + i];

                activeShapes.Add(new HitboxActiveShape {
                    Type = src.Type,
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
            CharacterState                                currentState,
            DynamicBuffer<HitboxPresetRange> presetRanges
        ) {
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