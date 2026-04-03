using Unity.Entities;

namespace Script.GamePlay.Unit.Interface {
    public interface IUnitManager {
        void RegisterUnit(Unit   unit, int team);
        void UnRegisterUnit(Unit unit);
        
        bool TryGetEntity(Unit   unit, out Entity entity);
    }
}