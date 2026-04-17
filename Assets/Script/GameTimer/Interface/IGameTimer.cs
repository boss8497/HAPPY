namespace Script.GameTimer {
    public interface IGameTimer {
        float Elapsed      { get; }
        float FixedElapsed { get; }
        float DeltaTime    { get; }
        float FixedTime    { get; }
        bool  IsPaused     { get; }


        void Pause();
        void Resume();
    }
}