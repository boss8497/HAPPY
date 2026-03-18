using System;

namespace Script.GamePlay.Character {
    [Flags]
    public enum CharacterState {
        None = 0,

        //초기화 완료
        Initialized = 1 << 0,
        Idling      = 1 << 1,
        Running     = 1 << 2,
        Jumping     = 1 << 3,
        Die         = 1 << 4,
    }
}