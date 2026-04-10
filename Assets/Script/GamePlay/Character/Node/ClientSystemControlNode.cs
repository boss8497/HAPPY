using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientSystemControlNode : ClientNodeBase {
        public ClientSystemControlNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) { }

        public override void Initialize() { }

        protected override void Enter() {
            _characterBehaviour.Character.SetAnimation(nameof(AnimationName.IDLE), true);
        }

        protected override UniTask Update(CancellationToken cts) {
            return UniTask.CompletedTask;
        }
    }
}