using System.Linq;
using R3;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Script.GameInfo.Character;
using Script.GamePlay.Character;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        public ReactiveProperty<DungeonProgress> DungeonProgress { get; private set; } = new();
        public ReactiveProperty<StageState>           State           { get; private set; } = new(StageState.None);
        public ReactiveProperty<int>                  PhaseIndex      { get; private set; } = new(0);

        public ReadOnlyReactiveProperty<bool> Initialized   { get; private set; }
        public ReadOnlyReactiveProperty<bool> SystemControl { get; private set; }


        public ReadOnlyReactiveProperty<DungeonInfo>            DungeonInfo { get; private set; }
        public ReadOnlyReactiveProperty<GameInfo.Dungeon.Stage> Stage       { get; private set; }
        public ReadOnlyReactiveProperty<PhaseInfo>              PhaseInfo   { get; private set; }


        private DisposableBag _reactiveDisposableBag;

        private void InitializeReactiveProperty(DungeonProgress dungeonProgress) {
            Initialized = State.Select(i => (i & StageState.Initialized) != 0)
                               .DistinctUntilChanged()
                               .ToReadOnlyReactiveProperty()
                               .AddTo(ref _reactiveDisposableBag);

            //Initialized가 true가 아니면 움직이면 안됨 Initialized == false 는 SystemControl과 동일
            SystemControl = State.CombineLatest(Initialized, (i, init) => !init || (i & StageState.SystemControl) != 0)
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

            PhaseInfo = PhaseIndex.CombineLatest(Stage, (index, stage) => index >= stage.phaseInfos.Length ? null : GameInfoManager.Instance.Get<PhaseInfo>(stage.phaseInfos[index]))
                                  .DistinctUntilChanged()
                                  .ToReadOnlyReactiveProperty()
                                  .AddTo(ref _reactiveDisposableBag);


            SystemControl.Subscribe((systemControl) => {
                             foreach (var character in _players) {
                                 if (systemControl) {
                                     character.AddState(CharacterState.SystemControl);
                                 }
                                 else {
                                     character.RemoveState(CharacterState.SystemControl);
                                 }
                             }
                             
                             foreach (var enemy in _enemies) {
                                 if (systemControl) {
                                     enemy.AddState(CharacterState.SystemControl);
                                 }
                                 else {
                                     enemy.RemoveState(CharacterState.SystemControl);
                                 }
                             }
                         })
                         .AddTo(ref _reactiveDisposableBag);


            DungeonProgress.OnNext(dungeonProgress);
            PhaseIndex.OnNext(0);
        }

        private void ReleaseReactiveProperty() {
            _reactiveDisposableBag.Dispose();
        }
        
        // 직접 호출 금지 AddState, RemoveState로 호출
        private void SetSystemControl(bool isOn) {
            if (isOn) {
                _systemControlStack += 1;
                Debug.Log($"사용자 입력 차단 시작. Stack: {_systemControlStack}");
            }
            else {
                _systemControlStack -= 1;
                Debug.Log($"사용자 입력 차단 해제. Stack: {_systemControlStack}");
            }

            if (_systemControlStack < 0) {
                _systemControlStack = 0;
            }

            if (_systemControlStack <= 0) {
                if (State.Value.HasFlag(StageState.SystemControl) == false) return;
                State.OnNext(State.Value &= ~StageState.SystemControl);
            }
            else {
                if (State.Value.HasFlag(StageState.SystemControl)) return;
                State.OnNext(State.Value |= StageState.SystemControl);
            }
        }
        
        public void ResetState() {
            _systemControlStack = 0;
            State.OnNext(StageState.None);
        }
        
        public void AddState(StageState state) {
            switch (state) {
                case StageState.SystemControl:
                    SetSystemControl(true);
                    return;
            }
            
            if (State.Value.HasFlag(state)) return;
            State.OnNext(State.Value |= state);
        }

        public void RemoveState(StageState state) {
            switch (state) {
                case StageState.SystemControl:
                    SetSystemControl(false);
                    return;
            }
            
            if (State.Value.HasFlag(state) == false) return;
            State.OnNext(State.Value &= ~state);
        }
    }
}