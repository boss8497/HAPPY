using R3;
using Script.GameInfo.Character;
using Script.GamePlay.ECS.Component;

//Reactive 필드 및 로직
namespace Script.GamePlay.Character {
    public partial class Character {
        //Initialize OnNext 순서대로
        public ReactiveProperty<CharacterState> State     { get; private set; } = new();
        public ReactiveProperty<double>         Health    { get; private set; } = new();
        public ReactiveProperty<double>         MaxHealth { get; private set; } = new();


        public ReadOnlyReactiveProperty<bool> Initialized   { get; private set; }
        public ReadOnlyReactiveProperty<bool> Jumping       { get; private set; }
        public ReadOnlyReactiveProperty<bool> Running       { get; private set; }
        public ReadOnlyReactiveProperty<bool> Die           { get; private set; }
        public ReadOnlyReactiveProperty<bool> SystemControl { get; private set; }


        private DisposableBag _reactiveDisposableBag;


        private void InitializeReactiveProperty() {
            _reactiveDisposableBag.Dispose();
            _reactiveDisposableBag = new();


            Health.Subscribe(health => {
                      if ((Die?.CurrentValue ?? true) || (Initialized?.CurrentValue ?? false) == false) return;

                      // 죽음
                      if (health <= 0) {
                          AddState(CharacterState.Die);
                          SetEnabledTag<UnitDieTag>(true);
                      }
                  })
                  .AddTo(ref _reactiveDisposableBag);


            Initialized = State.Select(i => (i & CharacterState.Initialized) != 0)
                               .DistinctUntilChanged()
                               .ToReadOnlyReactiveProperty()
                               .AddTo(ref _reactiveDisposableBag);

            Jumping = State.Select(i => (i & CharacterState.Jumping) != 0)
                           .DistinctUntilChanged()
                           .ToReadOnlyReactiveProperty()
                           .AddTo(ref _reactiveDisposableBag);

            Running = State.Select(i => (i & CharacterState.Running) != 0)
                           .DistinctUntilChanged()
                           .ToReadOnlyReactiveProperty()
                           .AddTo(ref _reactiveDisposableBag);

            Die = State.Select(i => (i & CharacterState.Die) != 0)
                       .DistinctUntilChanged()
                       .ToReadOnlyReactiveProperty()
                       .AddTo(ref _reactiveDisposableBag);

            SystemControl = State.CombineLatest(Initialized, (state, initialized) => !initialized || (state & CharacterState.SystemControl) != 0)
                                 .DistinctUntilChanged()
                                 .ToReadOnlyReactiveProperty()
                                 .AddTo(ref _reactiveDisposableBag);


            State.Subscribe((state) => { SyncCharacterHitboxEntity(); })
                 .AddTo(ref _reactiveDisposableBag);

            switch (CharacterInfo.type) {
                case CharacterType.Character:

                    SystemControl.CombineLatest(Initialized, (systemControl, initialized) => (systemControl, initialized))
                                 .Subscribe(data => {
                                     if (data.initialized == false) return;

                                     if (data.systemControl) {
                                         DisableRunning();
                                     }
                                     else {
                                         EnableRunning();
                                     }
                                 })
                                 .AddTo(ref _reactiveDisposableBag);

                    break;
            }


            State.OnNext(CharacterState.None);
            Health.OnNext(Status.Hp);
            MaxHealth.OnNext(Status.Hp);
        }

        private void ReleaseReactiveProperty() {
            _reactiveDisposableBag.Dispose();
        }
    }
}