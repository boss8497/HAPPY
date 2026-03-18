using R3;

//Reactive 필드 및 로직
namespace Script.GamePlay.Character {
    public partial class Character {
        public ReactiveProperty<double> Health { get; set; } = new();
        
        public ReadOnlyReactiveProperty<bool> IsDie { get; private set; }
        private DisposableBag _reactiveDisposableBag;
        
        
        
        
        private void InitializeReactiveProperty() {
            _reactiveDisposableBag.Dispose();
            _reactiveDisposableBag = new DisposableBag();

            IsDie = Health.Select(i => i <= 0)
                          .DistinctUntilChanged()
                          .ToReadOnlyReactiveProperty()
                          .AddTo(ref _reactiveDisposableBag);
            
            Health.OnNext(Status.Hp);
        }
        
        private void ReleaseReactiveProperty() {
            _reactiveDisposableBag.Dispose();
        }
    }
}