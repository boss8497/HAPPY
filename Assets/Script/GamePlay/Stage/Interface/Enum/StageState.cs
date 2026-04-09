using System;

namespace Script.GamePlay.Stage {
    [Flags]
    public enum StageState {
        None          = 0,
        Initialized   = 1 << 0,
        SystemControl = 1 << 1,
        Clear         = 1 << 2,
        Fail          = 1 << 3,
    }
}