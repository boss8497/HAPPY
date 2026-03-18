using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientWaitNode : ClientNodeBase {
        public ClientWaitNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) { }

        public override void Initialize() {
        }

        protected override void Enter() {
            _characterBehaviour.Character.SetAnimation(nameof(AnimationName.RUN), true);
        }

        protected override async UniTask Update(CancellationToken cts) {
            while (!cts.IsCancellationRequested) {
                
                await UniTask.DelayFrame(DefaultDelayFrame, cancellationToken: cts);
            }
        }
    }
}