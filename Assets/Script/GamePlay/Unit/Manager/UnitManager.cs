using System;
using System.Collections.Generic;
using Script.GamePlay.Unit.Interface;
using VContainer.Unity;

namespace Script.GamePlay.Unit {
    [System.Serializable]
    public class UnitManager : IUnitManager, IInitializable, IDisposable {
        private readonly Dictionary<long, Unit>       _units       = new();
        private readonly Dictionary<long, List<Unit>> _unitsByTeam = new();
        private readonly Queue<long>                  _returnIndex = new();

        private int _uidIndexer = 0;

        public void Initialize() { }

        public void RegisterUnit(Unit unit, int team) {
            unit.Set(NewUid(), team);
        }

        // 유닛이 진짜 삭제가 됐을때.
        // 유닛이 죽었을 때는 일단 캐싱
        public void UnRegisterUnit(Unit unit) {
            _returnIndex.Enqueue(unit.UID);

            _units.Remove(unit.UID);
            if (_unitsByTeam.TryGetValue(unit.Team, out List<Unit> units)) {
                units.Remove(unit);
            }

            unit.Set(-1, -1);
        }

        private long NewUid() {
            return _returnIndex.Count <= 0 ? (_uidIndexer++) : _returnIndex.Dequeue();
        }

        public void Dispose() {
            _units.Clear();
            _unitsByTeam.Clear();
            _returnIndex.Clear();
        }
    }
}