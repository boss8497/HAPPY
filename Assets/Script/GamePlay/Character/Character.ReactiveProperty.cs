using R3;
using Script.GameInfo.Character;

//Reactive 필드 및 로직
namespace Script.GamePlay.Character {
    public partial class Character {
        //Initialize OnNext 순서대로
        public ReactiveProperty<CharacterState> State       { get; set; } = new();
        public ReactiveProperty<double>         Health      { get; set; } = new();
        public ReactiveProperty<double>         MaxHealth   { get; set; } = new();
        
        
        public ReadOnlyReactiveProperty<bool> Initialized   { get; set; }
        public ReadOnlyReactiveProperty<bool> Jumping       { get; set; }
        public ReadOnlyReactiveProperty<bool> Running       { get; set; }
        public ReadOnlyReactiveProperty<bool> Die       { get; set; }
        public ReadOnlyReactiveProperty<bool> SystemControl { get; set; }


        private DisposableBag _reactiveDisposableBag;


        private void InitializeReactiveProperty() {
            _reactiveDisposableBag.Dispose();
            _reactiveDisposableBag = new();


            Health.Subscribe(health => {
                      if ((Die?.CurrentValue ?? true) || (Initialized?.CurrentValue ?? false) == false) return;

                      if (health <= 0) {
                          AddState(CharacterState.Die);
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



            State.OnNext(CharacterState.None);
            Health.OnNext(Status.Hp);
            MaxHealth.OnNext(Status.Hp);
        }

        private void ReleaseReactiveProperty() {
            _reactiveDisposableBag.Dispose();
        }
    }
}