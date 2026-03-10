using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Base;

namespace Script.GameInfo.Table.Interface {
    public interface IGameInfoManager {
        void    Load();
        UniTask LoadAsync();
        T       Get<T>(int uid) where T : InfoBase;
        T[] GetCollection<T>() where T : InfoBase;
        UniTask Flush();
        void    Dirty();
        void    Release();
    }
}