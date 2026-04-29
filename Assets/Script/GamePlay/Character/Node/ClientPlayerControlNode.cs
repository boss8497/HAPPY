using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.GamePlay.Input;
using Script.Utility.Runtime;

namespace Script.GamePlay.Character {
    [Serializable]
    public class ClientPlayerControlNode : ClientNodeBase, IClassPool {
        private IPlayerControls _controls;
        private Character       _character;

        public override ClientNodeBase Initialize(CharacterBehaviour characterBehaviour, NodeBase nodeBase) {
            base.Initialize(characterBehaviour, nodeBase);
            _controls  = _characterBehaviour.Character.PlayerControls;
            _character = _characterBehaviour.Character;
            return this;
        }

        protected override void Enter() {
            if (_controls.JumpPressed || _controls.JumpHeld) {
                _character.AddState(CharacterState.Jumping);
            }
        }

        protected override async UniTask Update(CancellationToken cts) {
            while (!cts.IsCancellationRequested && _controls.HasAnyInput) {
                if (_controls.JumpPressed || _controls.JumpHeld) {
                    CharacterBehaviour.Character.SyncJumpEntity();
                }

                //플레이어 조작을 컨트롤하기 때문에 DelayFrame을 사용하지 않고 Yield를 사용
                await UniTask.Yield();
            }
        }

        protected override void End() {
            CharacterBehaviour.Character.SyncJumpEntity();
        }

        public void OnRent() {
        }
        public void OnReturn() {
        }
    }
}