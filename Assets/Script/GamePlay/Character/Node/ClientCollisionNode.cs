using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    public class ClientCollisionNode : ClientNodeBase {
        private readonly Character _character;

        public ClientCollisionNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) {
            _character = characterBehaviour.Character;
        }

        public override void Initialize() {
        }

        protected override void Enter() {
            _character.RemoveState(CharacterState.Running);
        }

        protected override async UniTask Update(CancellationToken cts) {
            var time = _character.SetAnimation(nameof(AnimationName.DAMAGE));
            await UniTask.WaitForSeconds(time, cancellationToken: cts);
            _character.RemoveState(CharacterState.Collision);
        }
    }
}