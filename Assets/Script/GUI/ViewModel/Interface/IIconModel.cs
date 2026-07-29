using R3;

namespace Script.GUI.ViewModel {
    /// <summary>
    /// 아이콘 모델 인터페이스
    /// 아이템, 캐릭터 등 아이콘을 표시하는 모델에서 구현해야 하는 인터페이스입니다.
    /// </summary>
    public interface IIconModel : IViewModel {
        public ReadOnlyReactiveProperty<string> ImagePath { get; }
    }
}