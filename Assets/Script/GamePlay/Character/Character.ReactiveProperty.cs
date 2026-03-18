using R3;

//Reactive 필드 및 로직
namespace Script.GamePlay.Character {
    public partial class Character {
        public ReactiveProperty<double> Health    { get; set; } = new();
        public ReactiveProperty<double> MaxHealth { get; set; } = new();


        private DisposableBag _reactiveDisposableBag;


        private void InitializeReactiveProperty() {
            _reactiveDisposableBag.Dispose();
            _reactiveDisposableBag = new();


            Health.Subscribe(health => {
                      if (Die || Initialized == false) return;

                      if (health <= 0) {
                          AddState(CharacterState.Die);
                      }
                  })
                  .AddTo(ref _reactiveDisposableBag);


            Health.OnNext(Status.Hp);
            MaxHealth.OnNext(Status.Hp);
        }

        private void ReleaseReactiveProperty() {
            _reactiveDisposableBag.Dispose();
        }
    }
}