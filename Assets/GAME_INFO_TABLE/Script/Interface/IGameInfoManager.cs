using Cysharp.Threading.Tasks;
using Script.GameInfo.Base;
using Script.GameInfo.Info;

namespace Script.GameInfo.Table.Interface {
    public interface IGameInfoManager {
        ConfigurationInfo Config { get; }
        void              Load();
        UniTask           LoadAsync();
        T                 Get<T>(int uid) where T : InfoBase;
        T[]               GetCollection<T>() where T : InfoBase;
        UniTask           Flush();
        void              Dirty();
        void              Save<T>(T data) where T : InfoBase;
        void              Release();
    }
}