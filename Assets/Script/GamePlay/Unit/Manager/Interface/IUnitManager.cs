namespace Script.GamePlay.Unit.Interface {
    public interface IUnitManager {
        void RegisterUnit(Unit   unit, int team);
        void UnRegisterUnit(Unit unit);
    }
}