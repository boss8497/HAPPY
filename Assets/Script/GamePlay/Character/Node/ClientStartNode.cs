using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.Utility.Runtime;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientStartNode : ClientNodeBase, IClassPool {
        public override ClientNodeBase Initialize(CharacterBehaviour characterBehaviour, NodeBase nodeBase) {
            base.Initialize(characterBehaviour, nodeBase);
            return this;
        }

        protected override UniTask Update(CancellationToken cts) {
            return UniTask.CompletedTask;
        }

        public void OnRent() {
        }
        public void OnReturn() {
        }
    }
}