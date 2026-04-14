using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;

namespace Script.GUI.Screen.Interface {
    public interface IScreenManager {
        ScreenManagerState State       { get; }
        bool               Initialized { get; }

        void Initialize();
        UniTask OpenAsync(string key, CancellationToken ct = default);

        
        UniTask CloseAllAsync(bool force = false);
        UniTask Back();
        UniTask CloseAsync(ReadOnlyMemory<char> key,    bool force = false);
        UniTask CloseAsync(IScreen              screen, bool force = false);
    }
}