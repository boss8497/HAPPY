using R3;

// Model 접근과 Update 함수를 강제하기 위한 인터페이스
namespace Script.GameData.Data.Interface {
    public interface IData<T> {
        ReactiveProperty<T> Model { get; }
        
        void Update(T value);
    }
}