using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientSystemControlNode : ClientNodeBase {
        private Character _character;
        public ClientSystemControlNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) { }

        public override void Initialize() {
            _character = _characterBehaviour.Character;
        }

        protected override void Enter() {
            _character.RemoveState(CharacterState.Running);
            _character.SetAnimation(nameof(AnimationName.IDLE), true);
        }

        protected override async UniTask Update(CancellationToken cts) {
            await UniTask.WaitUntil(() => (_characterBehaviour.Character.SystemControl?.CurrentValue ?? true) == false, cancellationToken: cts);
        }
    }
}