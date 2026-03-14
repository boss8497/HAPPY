using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientStartNode : ClientNodeBase {
        
        public ClientStartNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) {
            
        }

        public override void Initialize() {
        }

        protected override UniTask Update(CancellationToken cts) {
            return UniTask.CompletedTask;
        }
    }
}