using System;

// GamePlay Character에서 사용하는 State
namespace Script.GameInfo.Character {
    [Flags]
    public enum CharacterState {
        None          = 0,
        Initialized   = 1 << 0, // 초기화 완료
        Idling        = 1 << 1, // Wait
        Running       = 1 << 2, // Run
        Jumping       = 1 << 3, // Jump
        Die           = 1 << 4, // Die
        SystemControl = 1 << 5, // System Pause
        Sliding       = 1 << 6,
        Collision     = 1 << 7,
        OutSideMap    = 1 << 8, // 맵 밖으로 나갔을때 추가되는 State 사실상 Player(Character Type)는 거의 쓸일이 없다.
    }
}