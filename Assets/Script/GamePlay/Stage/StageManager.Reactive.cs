using System.Linq;
using R3;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Script.GameInfo.Character;
using Script.GamePlay.Character;
using Unity.Plastic.Antlr3.Runtime;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        public ReactiveProperty<StageState> State      { get; private set; } = new(StageState.None);
        public ReactiveProperty<int>        PhaseIndex { get; private set; } = new(0);

        public ReactiveProperty<DungeonInfo>            DungeonInfo { get; private set; } = new();
        public ReactiveProperty<GameInfo.Dungeon.Stage> Stage       { get; private set; } = new();

        public ReactiveProperty<float> Score        { get; private set; } = new();
        public ReactiveProperty<float> RunningScore { get; private set; } = new();
        public ReactiveProperty<float> ItemScore    { get; private set; } = new();

        public ReadOnlyReactiveProperty<bool>      Initialized   { get; private set; }
        public ReadOnlyReactiveProperty<bool>      SystemControl { get; private set; }
        public ReadOnlyReactiveProperty<bool>      Fail          { get; private set; }
        public ReadOnlyReactiveProperty<bool>      Clear         { get; private set; }
        public ReadOnlyReactiveProperty<bool>      NextPhase     { get; private set; }
        public ReadOnlyReactiveProperty<bool>      ReStartState  { get; private set; }
        public ReadOnlyReactiveProperty<PhaseInfo> PhaseInfo     { get; private set; }


        private DisposableBag _reactiveDisposableBag;

        private void InitializeReactiveProperty(DungeonInfo dungeonInfo, GameInfo.Dungeon.Stage stage) {
            _reactiveDisposableBag = new();

            Initialized = State.Select(i => (i & StageState.Initialized) != 0)
                               .DistinctUntilChanged()
                               .ToReadOnlyReactiveProperty()
                               .AddTo(ref _reactiveDisposableBag);

            //Initialized가 true가 아니면 움직이면 안됨 Initialized == false 는 SystemControl과 동일
            SystemControl = State.CombineLatest(Initialized, (i, init) => !init || (i & StageState.SystemControl) != 0)
                                 .DistinctUntilChanged()
                                 .ToReadOnlyReactiveProperty()
                                 .AddTo(ref _reactiveDisposableBag);

            Fail = State.Select(i => (i & StageState.Fail) != 0)
                        .DistinctUntilChanged()
                        .ToReadOnlyReactiveProperty()
                        .AddTo(ref _reactiveDisposableBag);

            Clear = State.Select(i => (i & StageState.Clear) != 0)
                         .DistinctUntilChanged()
                         .ToReadOnlyReactiveProperty()
                         .AddTo(ref _reactiveDisposableBag);

            NextPhase = State.Select(i => (i & StageState.NextPhase) != 0)
                             .DistinctUntilChanged()
                             .ToReadOnlyReactiveProperty()
                             .AddTo(ref _reactiveDisposableBag);

            ReStartState = State.Select(i => (i & StageState.ReStart) != 0)
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

            Clear.SubscribeAwait(async (clear, ct) => {
                     if (clear) {
                         // 다음 Phase가 있는지 확인
                         if (Stage.CurrentValue.phaseInfos.Length - 1 > PhaseIndex.CurrentValue) {
                             RemoveState(StageState.Clear);
                             PhaseIndex.OnNext(PhaseIndex.CurrentValue + 1);

                             AddState(StageState.NextPhase);
                         }
                         else {
                             await Group.ClearedDungeon(DungeonInfo.CurrentValue, Stage.CurrentValue);
                             await _screenManager.OpenAsync(_clearScreenKey, ct);
                         }
                     }
                 })
                 .AddTo(ref _reactiveDisposableBag);

            Fail.SubscribeAwait(async (fail, ct) => {
                    if ((Initialized?.CurrentValue ?? false) == false) return;

                    if (fail) {
                        await _screenManager.OpenAsync(_failScreenKey, ct);
                    }
                })
                .AddTo(ref _reactiveDisposableBag);

            NextPhase.Subscribe(nextPhase => {
                         if (nextPhase) { }
                     })
                     .AddTo(ref _reactiveDisposableBag);

            RunningScore.CombineLatest(ItemScore, (runningScore, itemScore) => (runningScore, itemScore))
                        .Subscribe(score => { Score.OnNext(score.runningScore + score.itemScore); })
                        .AddTo(ref _reactiveDisposableBag);


            RunningScore.OnNext(0);
            ItemScore.OnNext(0);
            DungeonInfo.OnNext(dungeonInfo);
            Stage.OnNext(stage);
            PhaseIndex.OnNext(0);
        }

        private void ResetReactive() {
            _reactiveDisposableBag.Dispose();
        }

        private void ReleaseReactive() {
            ResetReactive();
            DungeonInfo.Dispose();
            Stage.Dispose();
            State.Dispose();
            PhaseIndex.Dispose();
            Score.Dispose();
            RunningScore.Dispose();
            ItemScore.Dispose();
        }


        // 직접 호출 금지 AddState, RemoveState로 호출
        private void SetSystemControl(bool isOn) {
            if (isOn) {
                _systemControlStack += 1;
                //Debug.Log($"사용자 입력 차단 시작. Stack: {_systemControlStack}");
            }
            else {
                _systemControlStack -= 1;
                //Debug.Log($"사용자 입력 차단 해제. Stack: {_systemControlStack}");
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
                // SystemControl은 Stack으로 관리해야하므로 별도 처리
                case StageState.SystemControl:
                    SetSystemControl(true);
                    return;
            }

            if (State.Value.HasFlag(state)) return;
            State.OnNext(State.Value |= state);
        }

        public void RemoveState(StageState state) {
            switch (state) {
                // SystemControl은 Stack으로 관리해야하므로 별도 처리
                case StageState.SystemControl:
                    SetSystemControl(false);
                    return;
            }

            if (State.Value.HasFlag(state) == false) return;
            State.OnNext(State.Value &= ~state);
        }
    }
}