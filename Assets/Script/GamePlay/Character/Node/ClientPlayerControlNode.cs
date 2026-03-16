using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info.Character;

namespace Script.GamePlay.Character {
    public class ClientPlayerControlNode : ClientNodeBase {
        public ClientPlayerControlNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) { }
        public override void Initialize() { }

        protected override UniTask Update(CancellationToken cts) {
            return UniTask.CompletedTask;
        }
    }
}