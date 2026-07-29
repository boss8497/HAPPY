using R3;

namespace Script.GUI.ViewModel {
    public interface IInfoModel : IViewModel {
        public ReadOnlyReactiveProperty<int> InfoUid { get; }
    }
}