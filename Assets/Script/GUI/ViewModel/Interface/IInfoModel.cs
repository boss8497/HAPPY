using R3;

namespace Script.GUI.ViewModel {
    public interface IInfoModel {
        public ReadOnlyReactiveProperty<int> InfoUid { get; }
    }
}