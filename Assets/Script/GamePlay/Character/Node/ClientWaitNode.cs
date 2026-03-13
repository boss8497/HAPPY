using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientWaitNode : ClientNodeBase {
        public ClientWaitNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) { }

        public override void Initialize() {
        }

        protected override UniTask Update(CancellationToken cts) {
            throw new System.NotImplementedException();
        }
    }
}