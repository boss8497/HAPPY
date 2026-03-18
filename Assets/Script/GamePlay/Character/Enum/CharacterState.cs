using System;

namespace Script.GamePlay.Character {
    [Flags]
    public enum CharacterState {
        None = 0,
        Idle = 1 << 0,
        Move = 1 << 1,
        Jump = 1 << 2,
        Die  = 1 << 3,
    }
}