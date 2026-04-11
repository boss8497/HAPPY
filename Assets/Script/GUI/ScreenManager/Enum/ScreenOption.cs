using System;

namespace Script.GUI.Screen.Enum {
    [Flags]
    public enum ScreenOption {
        None      = 0,
        DontClose = 1 << 0,
    }
}