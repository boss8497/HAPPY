using System;
using System.Collections.Generic;
using Script.GamePlay.ECS.Component;
using Script.GamePlay.ECS.World;
using Script.GamePlay.Unit.Interface;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VContainer;
using VContainer.Unity;

namespace Script.GamePlay.Unit {
    [Serializable]
    public class UnitManager : IUnitManager, IInitializable, IDisposable {
        private readonly Dictionary<long, Unit>      _units         = new();
        private readonly Dictionary<long, Entity>    _entitiesByUid = new();
        private readonly Dictionary<int, List<Unit>> _unitsByTeam   = new();
        private readonly Queue<long>                 _returnIndex   = new();

        private readonly IStageEntityWorld _stageEntityWorld;
        private          int               _uidIndexer = 0;

        [Inject]
        public UnitManager(IStageEntityWorld stageEntityWorld) {
            _stageEntityWorld = stageEntityWorld;
        }

        public void Initialize() { }

        public void RegisterUnit(Unit unit, int team) {
            if (unit == null)
                throw new ArgumentNullException($"Unit cannot be null.");

            if (_stageEntityWorld.IsAlive == false)
                throw new ArgumentNullException($"월드가 생성되지 않았습니다.");

            var uid = NewUid();
            unit.Set(uid, team);

            _units[uid] = unit;

            if (_unitsByTeam.TryGetValue(team, out var units) == false) {
                units = new List<Unit>();
                _unitsByTeam.Add(team, units);
            }

            if (units.Contains(unit) == false) {
                units.Add(unit);
            }

            CreateEntity(unit);
        }

        public void UnRegisterUnit(Unit unit) {
            if (unit == null)
                return;

            DestroyEntity(unit);

            _returnIndex.Enqueue(unit.UID);
            _units.Remove(unit.UID);

            if (_unitsByTeam.TryGetValue(unit.Team, out var units)) {
                units.Remove(unit);

                if (units.Count <= 0) {
                    _unitsByTeam.Remove(unit.Team);
                }
            }

            unit.Set(-1, -1);
        }

        public bool TryGetEntity(Unit unit, out Entity entity) {
            entity = Entity.Null;

            if (unit == null)
                return false;

            if (_stageEntityWorld.IsAlive == false)
                return false;

            if (_entitiesByUid.TryGetValue(unit.UID, out entity) == false)
                return false;

            return _stageEntityWorld.EntityManager.Exists(entity);
        }

        private void CreateEntity(Unit unit) {
            var entityManager = _stageEntityWorld.EntityManager;

            if (_entitiesByUid.TryGetValue(unit.UID, out var entity) == false || entityManager.Exists(entity) == false) {
                entity                   = entityManager.CreateEntity();
                _entitiesByUid[unit.UID] = entity;
            }

            EnsureCommonComponents(entityManager, entity);

            entityManager.SetComponentData(entity, new LocalTransform {
                Position = unit.Transform.position,
                Rotation = unit.Transform.rotation,
                Scale    = unit.Transform.localScale.x,
            });

            entityManager.SetComponentData(entity, new UnitData {
                Uid        = unit.UID,
                Team       = unit.Team,
                InstanceId = unit.GetInstanceID(),
                GameObject = unit.gameObject,
            });
        }

        private static void EnsureCommonComponents(EntityManager entityManager, Entity entity) {
            if (entityManager.HasComponent<LocalTransform>(entity) == false) {
                entityManager.AddComponent<LocalTransform>(entity);
            }

            if (entityManager.HasComponent<UnitEntityTag>(entity) == false) {
                entityManager.AddComponent<UnitEntityTag>(entity);
            }

            if (entityManager.HasComponent<UnitData>(entity) == false) {
                entityManager.AddComponentData(entity, default(UnitData));
            }
        }

        private void DestroyEntity(Unit unit) {
            if (unit == null)
                return;

            if (_stageEntityWorld.IsAlive == false)
                return;

            if (_entitiesByUid.TryGetValue(unit.UID, out var entity)) {
                var entityManager = _stageEntityWorld.EntityManager;

                if (entityManager.Exists(entity)) {
                    entityManager.DestroyEntity(entity);
                }

                _entitiesByUid.Remove(unit.UID);
            }
        }

        public bool TryGetUnit(long uid, out Unit unit) {
            if (_units.TryGetValue(uid, out unit) == false)
                return false;

            return true;
        }

        private long NewUid() {
            return _returnIndex.Count <= 0 ? _uidIndexer++ : _returnIndex.Dequeue();
        }

        public void Dispose() {
            _units.Clear();
            _entitiesByUid.Clear();
            _unitsByTeam.Clear();
            _returnIndex.Clear();
        }
    }
}