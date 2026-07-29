using R3;

namespace Script.GUI.ViewModel {
    public interface IViewModel {
        public ReactiveProperty<ViewModelState> State         { get; }
        
        public ReadOnlyReactiveProperty<bool>   IsInitialized { get; }
    }
}