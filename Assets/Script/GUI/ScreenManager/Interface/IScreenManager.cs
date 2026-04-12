using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;

namespace Script.GUI.Screen.Interface {
    public interface IScreenManager {
        ScreenManagerState State       { get; }
        bool               Initialized { get; }

        void Initialize();

        UniTask OpenAsync(string key);
    }
}