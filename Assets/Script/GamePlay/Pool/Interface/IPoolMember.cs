namespace Script.GamePlay.Pool {
    public interface IPoolMember {
        string Key { get; }
        void   Set(IGameObjectPool gameObjectPool);
    }
}