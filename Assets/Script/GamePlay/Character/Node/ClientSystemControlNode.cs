using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.Utility.Runtime;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientSystemControlNode : ClientNodeBase, IClassPool {
        private Character _character;
        public override ClientNodeBase Initialize(CharacterBehaviour characterBehaviour, NodeBase nodeBase) {
            base.Initialize(characterBehaviour, nodeBase);
            _character = _characterBehaviour.Character;
            return this;
        }

        protected override void Enter() {
            _character.RemoveState(CharacterState.Running);
        }

        protected override async UniTask Update(CancellationToken cts) {
            await UniTask.WaitUntil(() => (_characterBehaviour.Character.SystemControl?.CurrentValue ?? true) == false, cancellationToken: cts);
        }

        public void OnRent() {
        }
        public void OnReturn() {
        }
    }
}