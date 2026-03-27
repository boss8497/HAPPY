using System;

namespace Script.GUI.Screen.Enum {
    [Flags]
    public enum ScreenManagerState {
        None        = 0,
        Initialized = 1 << 0,
        Loading     = 1 << 1,
    }
}