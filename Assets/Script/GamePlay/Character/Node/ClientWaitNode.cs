using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientWaitNode : ClientNodeBase {
        private Character _character;
        public ClientWaitNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) { }

        public override void Initialize() {
            _character = _characterBehaviour.Character;
        }

        protected override void Enter() {
            _character.AddState(CharacterState.Running);
            _character.SetAnimation(nameof(AnimationName.RUN), true);
        }

        protected override async UniTask Update(CancellationToken cts) {
            while (!cts.IsCancellationRequested) {
                
                await UniTask.DelayFrame(DefaultDelayFrame, cancellationToken: cts);
            }
        }
    }
}