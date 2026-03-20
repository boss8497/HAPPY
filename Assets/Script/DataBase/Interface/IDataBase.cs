using Cysharp.Threading.Tasks;
using Script.DataBase.Enum;

namespace Script.DataBase.Interface {
    /// <summary>
    /// 유저 데이터를 저장할 인터페이스
    /// 현재는 따로 서버 구현 예정이 없기 때문에 json 으로 저장 목표
    /// 테스트 시 json으로 저장하고 나중에는 MessagePack으로 변경할 예정
    /// </summary>
    public interface IDataBase {
        bool Initialized { get; }

        UniTask<T> LoadAsync<T>(string path, DataType type                = DataType.Json);
        UniTask    SaveAsync<T>(string path, T        data, DataType type = DataType.Json);
        bool       Exists(string       path);
    }
}