using System.Linq;
using R3;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        public ReactiveProperty<DungeonProgress> DungeonProgress { get; private set; } = new();
        public ReactiveProperty<StageState>      State           { get; private set; } = new(StageState.None);

        public ReadOnlyReactiveProperty<bool> Initialized   { get; private set; }
        public ReadOnlyReactiveProperty<bool> SystemControl { get; private set; }


        public ReadOnlyReactiveProperty<DungeonInfo>            DungeonInfo { get; private set; }
        public ReadOnlyReactiveProperty<GameInfo.Dungeon.Stage> Stage       { get; private set; }


        private DisposableBag _reactiveDisposableBag;

        private void InitializeReactiveProperty(DungeonProgress dungeonProgress, StageState state) {
            Initialized = State.Select(i => (i & StageState.Initialized) != 0)
                               .DistinctUntilChanged()
                               .ToReadOnlyReactiveProperty()
                               .AddTo(ref _reactiveDisposableBag);

            SystemControl = State.Select(i => (i & StageState.SystemControl) != 0)
                                 .DistinctUntilChanged()
                                 .ToReadOnlyReactiveProperty()
                                 .AddTo(ref _reactiveDisposableBag);

            DungeonInfo = DungeonProgress.Select(i => i == null ? null : GameInfoManager.Instance.Get<DungeonInfo>(i.dungeonUid))
                                         .DistinctUntilChanged()
                                         .ToReadOnlyReactiveProperty()
                                         .AddTo(ref _reactiveDisposableBag);

            Stage = DungeonProgress.CombineLatest(DungeonInfo, (progress, info) => {
                                       if (progress == null || info == null) return null;
                                       return info.stages.FirstOrDefault(r => r.guid.Value == progress.stageGuid);
                                   })
                                   .DistinctUntilChanged()
                                   .ToReadOnlyReactiveProperty()
                                   .AddTo(ref _reactiveDisposableBag);


            DungeonProgress.OnNext(dungeonProgress);
            State.OnNext(state);
        }

        private void ReleaseReactiveProperty() {
            _reactiveDisposableBag.Dispose();
        }

        public void AddState(StageState state) {
            if (State.Value.HasFlag(state)) return;
            State.OnNext(State.Value |= state);
        }

        public void RemoveState(StageState state) {
            if (State.Value.HasFlag(state) == false) return;
            State.OnNext(State.Value &= ~state);
        }
    }
}