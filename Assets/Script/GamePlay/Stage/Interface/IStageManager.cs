using R3;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;

namespace Script.GamePlay.Stage {
    public interface IStageManager {
        ReactiveProperty<StageState>                     State         { get; }
        ReadOnlyReactiveProperty<bool>                   Initialized   { get; }
        ReadOnlyReactiveProperty<bool>                   SystemControl { get; }
        ReadOnlyReactiveProperty<DungeonInfo>            DungeonInfo   { get; }
        ReadOnlyReactiveProperty<GameInfo.Dungeon.Stage> Stage         { get; }

        void Initialize(DungeonProgress dungeonProgress);
        void Begin();
        void Start();
        void End();
        void Release();
    }
}