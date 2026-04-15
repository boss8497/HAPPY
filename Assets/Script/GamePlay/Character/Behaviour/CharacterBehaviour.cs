using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.Utility.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Script.GamePlay.Character {
    [Serializable]
    public class CharacterBehaviour : IClassPool {
        private BehaviourInfo _info;
        private Character     _character;

        private ClientNodeBase[]                 _nodes;
        private Dictionary<Guid, ClientNodeBase> _nodesByGuid;


        [SerializeReference, ShowInInspector]
        private ClientNodeBase          _currentNode;
        private CancellationTokenSource _nodeCts;


        //public
        public Character Character => _character;


        public void Initialize(BehaviourInfo info, Character character) {
            _info      = info;
            _character = character;

            _nodes = info.nodes.Select(n => {
                             var node = Create(this, n);
                             node.Initialize();
                             return node;
                         })
                         .ToArray();
            _nodesByGuid = _nodes.ToDictionary(r => r.NodeBase.guid.Value, r => r);
        }

        public void Start() {
            _currentNode = _nodes.FirstOrDefault(r => r is ClientStartNode);
            if (_currentNode == null) {
                Debug.LogError($"시작 노드를 찾지 못했습니다. 시작 노드를 추가해주세요.");
                return;
            }

            _nodeCts = new();
            _currentNode.Start(_nodeCts.Token).Forget();
        }

        public async UniTask StopAsync() {
            if (_nodeCts is { IsCancellationRequested: false }) {
                _nodeCts.Cancel();
                await UniTask.WaitWhile(() => _nodes.All(a => !a.IsPlay));
                _nodeCts.Dispose();
                _nodeCts = null;
            }
        }
        public void Stop() {
            if (_nodeCts is { IsCancellationRequested: false }) {
                _nodeCts.Cancel();
                _nodeCts.Dispose();
                _nodeCts = null;
            }
        }


        public static ClientNodeBase Create(CharacterBehaviour characterBehaviour, NodeBase nodeBase) {
            var type = nodeBase.GetType();

            return type switch {
                var t when t == typeof(StartNode)         => new ClientStartNode(characterBehaviour, nodeBase),
                var t when t == typeof(WaitNode)          => new ClientWaitNode(characterBehaviour, nodeBase),
                var t when t == typeof(DieNode)           => new ClientDieNode(characterBehaviour, nodeBase),
                var t when t == typeof(PlayerControlNode) => new ClientPlayerControlNode(characterBehaviour, nodeBase),
                var t when t == typeof(SystemControlNode) => new ClientSystemControlNode(characterBehaviour, nodeBase),
                _                                         => null
            };
        }

        public async UniTask OnTransition(ClientNodeBase node, ClientTransitionBase transition) {
            await StopAsync();
            
            node.Stop();

            if (_nodesByGuid.TryGetValue(transition.NextNodeGuid, out var nextNode) == false) {
                Debug.LogError($"다음 노드를 찾지 못했습니다. 노드 UID: {node.NodeBase.id}");
                return;
            }

            _currentNode = nextNode;
            _nodeCts     = new();
            _currentNode.Start(_nodeCts.Token).Forget();
        }

        //<summary> Called when the character is rented from the pool. </summary>
        public void OnRent()   { }
        public void OnReturn() { }
    }
}