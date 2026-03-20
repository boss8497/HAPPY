using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    [Serializable]
    public class ClientDieNode : ClientNodeBase {
        public ClientDieNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) { }
        public override void Initialize() { }

        protected override UniTask Update(CancellationToken cts) {
            return UniTask.CompletedTask;
        }
    }
}