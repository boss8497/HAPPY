using System;
using UnityEngine;

namespace Script.GamePlay.Character {
    public interface IPlayerControls : IDisposable {
        Vector2 Move         { get; }
        bool    JumpPressed  { get; }
        bool    JumpHeld     { get; }
        bool    JumpReleased { get; }
        bool    HasMoveInput { get; }
        bool    HasAnyInput  { get; }

        void Initialize();
        bool ConsumeJumpPressed();
        bool ConsumeJumpReleased();
    }
}