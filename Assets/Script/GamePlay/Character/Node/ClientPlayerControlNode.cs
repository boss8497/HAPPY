using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.GamePlay.Input;

namespace Script.GamePlay.Character {
    [Serializable]
    public class ClientPlayerControlNode : ClientNodeBase {
        private IPlayerControls _controls;
        private Character       _character;


        public ClientPlayerControlNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) { }

        public override void Initialize() {
            _controls  = _characterBehaviour.Character.PlayerControls;
            _character = _characterBehaviour.Character;
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
    }
}