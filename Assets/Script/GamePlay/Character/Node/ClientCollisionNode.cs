using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.Utility.Runtime;

namespace Script.GamePlay.Character {
    public class ClientCollisionNode : ClientNodeBase, IClassPool {
        private Character _character;

        public override ClientNodeBase Initialize(CharacterBehaviour characterBehaviour, NodeBase nodeBase) {
            base.Initialize(characterBehaviour, nodeBase);
            _character = characterBehaviour.Character;
            return this;
        }

        protected override void Enter() {
            _character.RemoveState(CharacterState.Running);
            _character.StopJumEntity();
        }

        protected override async UniTask Update(CancellationToken cts) {
            var time = _character.SetAnimation(nameof(AnimationName.DAMAGE));
            await UniTask.WaitForSeconds(time, cancellationToken: cts);
            _character.RemoveState(CharacterState.Collision);
        }

        public void OnRent() {
        }
        public void OnReturn() {
        }
    }
}