using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using UnityEngine;

namespace Script.GamePlay.Character {
    [Serializable]
    public class ClientDieNode : ClientNodeBase {
        public ClientDieNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) { }

        public override void Initialize() {
        }

        protected override UniTask Update(CancellationToken cts) {
            Debug.LogError($"죽었어!!! {CharacterBehaviour.Character.CharacterInfo.Name}");
            return UniTask.CompletedTask;
        }
    }
}