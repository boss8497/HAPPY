
/// <summary>
/// 유저 데이터를 저장할 인터페이스
/// 현재는 따로 서버 구현 예정이 없기 때문에 Json으로 저장 목표
/// </summary>
namespace Script.DataBase.Interface {
    public interface IRepository {
        void Initialize();
    }
}