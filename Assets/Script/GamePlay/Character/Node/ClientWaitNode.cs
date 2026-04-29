using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.Utility.Runtime;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientWaitNode : ClientNodeBase, IClassPool {
        private Character _character;
        public override ClientNodeBase Initialize(CharacterBehaviour characterBehaviour, NodeBase nodeBase) {
            base.Initialize(characterBehaviour, nodeBase);
            _character = _characterBehaviour.Character;
            return this;
        }

        protected override void Enter() {
            _character.AddState(CharacterState.Idling);
            _character.SetAnimation(nameof(AnimationName.IDLE), true);
        }

        protected override UniTask Update(CancellationToken cts) {
            return UniTask.CompletedTask;
        }

        public void OnRent() {
        }
        public void OnReturn() {
            _character = null;
        }
    }
}