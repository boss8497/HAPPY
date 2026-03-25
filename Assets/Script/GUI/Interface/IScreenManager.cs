using Script.GUI.Enum;

namespace Script.GUI.Interface {
    public interface IScreenManager {
        ScreenManagerState State       { get; }
        bool               Initialized { get; }

        void Initialize();
    }
}