using Cysharp.Threading.Tasks;

namespace Script.GameInfo.Table.Interface {
    public interface IGameInfoManager {
        IGameInfoManager Instance { get; }

        void    Load();
        UniTask LoadAsync();
        UniTask Flush();
        void    Dirty();
        void    Release();
    }
}