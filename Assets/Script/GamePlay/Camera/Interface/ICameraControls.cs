namespace Script.GamePlay.Camera {
    public interface ICameraControls {
        UnityEngine.Camera MainCamera   { get; }
        float              OutSideLeftX { get; }
        float              InSideLeftX  { get; }
        float              SpawnOffset  { get; }
    }
}