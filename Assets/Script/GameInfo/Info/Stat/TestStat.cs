using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Script.GameInfo.Info.Stat {

    namespace Game.Stats {
        /// <summary>
        /// Modifier 연산 순서(파이프라인 단계).
        /// Base -> Add -> Mul -> PostAdd (Final)
        /// </summary>
        public enum StatOp : byte {
            Add      = 0, // base에 더해짐
            Mul      = 1, // (base+add)에 곱해짐 (예: +10% => value=0.10, 곱은 (1+sum))
            PostAdd  = 2, // 마지막에 더해짐(예: 최종 피해 +5 같은)
            Override = 3, // 특정 값으로 덮어쓰기(최우선/우선순위 기반)
        }

        /// <summary>
        /// 하나의 스탯에 적용되는 수정자.
        /// 값(value)의 의미는 Op에 따라 달라짐.
        /// - Add/PostAdd: 그대로 더함
        /// - Mul: 보통 0.10 = +10% 로 해석해 (1+sumMul)로 적용
        /// - Override: 최종값을 value로 설정(우선순위 높은 것)
        ///
        /// ExpiresAt: 만료시간(초). 만료 없으면 float.PositiveInfinity 권장.
        /// SourceId: 같은 출처의 modifier를 한 번에 제거하기 위한 키(예: 장비 instance id, 버프 id 등)
        /// </summary>
        public readonly struct StatModifier {
            public readonly int    SourceId;
            public readonly StatOp Op;
            public readonly float  Value;
            public readonly int    Priority;
            public readonly float  ExpiresAt; // Time.time 기반 권장

            public StatModifier(int sourceId, StatOp op, float value, int priority = 0, float expiresAt = float.PositiveInfinity) {
                SourceId  = sourceId;
                Op        = op;
                Value     = value;
                Priority  = priority;
                ExpiresAt = expiresAt;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsExpired(float now) => now >= ExpiresAt;
        }

        /// <summary>
        /// 단일 스탯 컨테이너(특정 StatId용).
        /// 내부 modifier 리스트를 유지하며 Dirty 시에만 재계산.
        /// </summary>
        public sealed class Stat {
            public int Id { get; }

            public float BaseValue {
                get => _baseValue;
                set {
                    if (Math.Abs(_baseValue - value) < 0.000001f) return;
                    _baseValue = value;
                    _dirty     = true;
                }
            }

            private float _baseValue;
            private float _cachedFinal;
            private bool  _dirty;

            // modifier 리스트(할당 최소화를 위해 List 유지)
            private readonly List<StatModifier> _mods = new List<StatModifier>(8);

            // 만료 체크 최적화: "다음 만료 시간" 힌트
            private float _nextExpiryAt = float.PositiveInfinity;

            public Stat(int id, float baseValue = 0f) {
                Id         = id;
                _baseValue = baseValue;
                _dirty     = true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetFinal(float now) {
                // 시간이 지나 만료가 생겼을 수도 있으니, nextExpiryAt 기준으로만 빠르게 체크
                if (now >= _nextExpiryAt)
                    RemoveExpiredInternal(now);

                if (_dirty)
                    RecalculateInternal(now);

                return _cachedFinal;
            }

            public void AddModifier(in StatModifier mod) {
                _mods.Add(mod);
                if (mod.ExpiresAt < _nextExpiryAt) _nextExpiryAt = mod.ExpiresAt;
                _dirty = true;
            }

            /// <summary>
            /// SourceId 기준으로 modifier 제거(장비 해제/버프 해제에 유용)
            /// </summary>
            public int RemoveBySource(int sourceId, float now) {
                if (_mods.Count == 0) return 0;

                int removed = 0;
                for (int i = _mods.Count - 1; i >= 0; --i) {
                    if (_mods[i].SourceId == sourceId) {
                        _mods.RemoveAt(i);
                        removed++;
                    }
                }

                if (removed > 0) {
                    RebuildNextExpiry(now);
                    _dirty = true;
                }

                return removed;
            }

            /// <summary>
            /// 특정 조건으로 modifier 제거(필요 시 사용)
            /// </summary>
            public int RemoveWhere(Func<StatModifier, bool> predicate, float now) {
                if (_mods.Count == 0) return 0;
                int removed = 0;

                for (int i = _mods.Count - 1; i >= 0; --i) {
                    if (predicate(_mods[i])) {
                        _mods.RemoveAt(i);
                        removed++;
                    }
                }

                if (removed > 0) {
                    RebuildNextExpiry(now);
                    _dirty = true;
                }

                return removed;
            }

            /// <summary>
            /// 외부에서 Tick을 돌리기 싫다면, GetFinal에서 nextExpiryAt로 자동 처리.
            /// 그래도 "프레임마다 업데이트"하고 싶으면 이걸 호출해도 됨.
            /// </summary>
            public void Tick(float now) {
                if (now >= _nextExpiryAt)
                    RemoveExpiredInternal(now);

                // Tick에서 재계산까지 강제할지 여부는 취향. 여기선 안 함.
            }

            public IReadOnlyList<StatModifier> Modifiers => _mods;

            // ------------------ Internals ------------------

            private void RemoveExpiredInternal(float now) {
                if (_mods.Count == 0) {
                    _nextExpiryAt = float.PositiveInfinity;
                    return;
                }

                bool removedAny = false;
                for (int i = _mods.Count - 1; i >= 0; --i) {
                    if (_mods[i].IsExpired(now)) {
                        _mods.RemoveAt(i);
                        removedAny = true;
                    }
                }

                if (removedAny) {
                    RebuildNextExpiry(now);
                    _dirty = true;
                }
                else {
                    // 힌트가 잘못되었을 수 있으니 재계산
                    RebuildNextExpiry(now);
                }
            }

            private void RebuildNextExpiry(float now) {
                float next = float.PositiveInfinity;
                for (int i = 0; i < _mods.Count; i++) {
                    float e                       = _mods[i].ExpiresAt;
                    if (e > now && e < next) next = e;
                }

                _nextExpiryAt = next;
            }

            private void RecalculateInternal(float now) {
                // 만료 정리 한 번 더(외부에서 Base 변경만 하고 GetFinal로 바로 들어온 케이스)
                if (now >= _nextExpiryAt)
                    RemoveExpiredInternal(now);

                float basePlusAdd = _baseValue;
                float sumMul      = 0f;
                float postAdd     = 0f;

                // Override는 "우선순위 가장 높은 것 1개"만 적용 (필요 시 정책 변경 가능)
                bool  hasOverride   = false;
                int   bestPrio      = int.MinValue;
                float overrideValue = 0f;

                // 주의: 여기서 정렬을 매번 하지 않도록
                // Override만 priority 비교로 1개 선정, 나머지는 합산.
                for (int i = 0; i < _mods.Count; i++) {
                    var m = _mods[i];
                    if (m.IsExpired(now)) continue; // nextExpiryAt 힌트 밖에서 호출될 수 있어 안전장치

                    switch (m.Op) {
                        case StatOp.Add:
                            basePlusAdd += m.Value;
                            break;
                        case StatOp.Mul:
                            sumMul += m.Value;
                            break;
                        case StatOp.PostAdd:
                            postAdd += m.Value;
                            break;
                        case StatOp.Override:
                            if (!hasOverride || m.Priority > bestPrio) {
                                hasOverride   = true;
                                bestPrio      = m.Priority;
                                overrideValue = m.Value;
                            }

                            break;
                    }
                }

                float finalValue = hasOverride
                                       ? overrideValue
                                       : (basePlusAdd * (1f + sumMul) + postAdd);

                _cachedFinal = finalValue;
                _dirty       = false;
            }
        }

        /// <summary>
        /// 여러 스탯을 가진 엔티티(캐릭터/아이템/펫 등)의 스탯 컨테이너.
        /// StatId(int) 기반이라 스탯 종류 확장에 강함.
        /// </summary>
        public sealed class StatSet {
            private readonly Dictionary<int, Stat> _stats = new Dictionary<int, Stat>(32);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Stat GetOrCreate(int statId, float defaultBase = 0f) {
                if (_stats.TryGetValue(statId, out var stat))
                    return stat;

                stat = new Stat(statId, defaultBase);
                _stats.Add(statId, stat);
                return stat;
            }

            public bool TryGet(int statId, out Stat stat) => _stats.TryGetValue(statId, out stat);

            public float GetFinal(int statId, float now, float defaultBaseIfMissing = 0f) {
                return GetOrCreate(statId, defaultBaseIfMissing).GetFinal(now);
            }

            public void SetBase(int statId, float baseValue) {
                GetOrCreate(statId).BaseValue = baseValue;
            }

            public void AddModifier(int statId, in StatModifier mod) {
                GetOrCreate(statId).AddModifier(mod);
            }

            /// <summary>
            /// SourceId 기준으로 전체 스탯에서 modifier 제거(장비/버프 전체 제거에 매우 유용)
            /// </summary>
            public int RemoveBySourceAll(int sourceId, float now) {
                int removed = 0;
                foreach (var kv in _stats)
                    removed += kv.Value.RemoveBySource(sourceId, now);
                return removed;
            }

            public void Tick(float now) {
                foreach (var kv in _stats)
                    kv.Value.Tick(now);
            }
        }

        // ------------------ Example StatId ------------------
        // 게임마다 여기만 바꿔서 공용으로 쓰면 됨.
        public static class StatIds {
            public const int Hp         = 1;
            public const int Atk        = 2;
            public const int Def        = 3;
            public const int CritChance = 4;
            public const int MoveSpeed  = 5;
        }
    }
}