using System;

// GamePlay Character에서 사용하는 State
namespace Script.GameInfo.Character {
    [Flags]
    public enum CharacterState {
        None = 0,

        //초기화 완료
        Initialized   = 1 << 0,
        Idling        = 1 << 1,
        Running       = 1 << 2,
        Jumping       = 1 << 3,
        Die           = 1 << 4,
        SystemControl = 1 << 5,
        Sliding       = 1 << 6,
        Collision     = 1 << 7,
    }
}