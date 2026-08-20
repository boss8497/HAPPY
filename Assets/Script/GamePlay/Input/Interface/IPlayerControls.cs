using System;
using UnityEngine;

namespace Script.GamePlay.Input {
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

        /// <summary>
        /// 모바일 온스크린 버튼 등, InputAction 바인딩을 거치지 않는 입력에서 Jump를 눌렀을 때 호출.
        /// </summary>
        void PressJump();

        /// <summary>
        /// <see cref="PressJump"/>와 대응. 뗐을 때 호출.
        /// </summary>
        void ReleaseJump();
    }
}