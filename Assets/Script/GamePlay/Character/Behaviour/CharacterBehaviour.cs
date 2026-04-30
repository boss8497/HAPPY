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
        private ClientNodeBase _currentNode;

        private CancellationTokenSource _nodeCts;


        //public
        public Character Character => _character;


        public void Initialize(BehaviourInfo info, Character character) {
            _info      = info;
            _character = character;

            _nodes = info.nodes.Select(n => {
                             var node = Create(this, n);
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

        public UniTask StopAsync() {
            if (_nodeCts is { IsCancellationRequested: false }) {
                _nodeCts.Cancel();
                _nodeCts.Dispose();
                _nodeCts = null;
            }

            return UniTask.CompletedTask;
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
                var t when t == typeof(StartNode)         => ClassPool.Get<ClientStartNode>().Initialize(characterBehaviour, nodeBase),
                var t when t == typeof(WaitNode)          => ClassPool.Get<ClientWaitNode>().Initialize(characterBehaviour, nodeBase),
                var t when t == typeof(RunNode)           => ClassPool.Get<ClientRunNode>().Initialize(characterBehaviour, nodeBase),
                var t when t == typeof(DieNode)           => ClassPool.Get<ClientDieNode>().Initialize(characterBehaviour, nodeBase),
                var t when t == typeof(PlayerControlNode) => ClassPool.Get<ClientPlayerControlNode>().Initialize(characterBehaviour, nodeBase),
                var t when t == typeof(SystemControlNode) => ClassPool.Get<ClientSystemControlNode>().Initialize(characterBehaviour, nodeBase),
                var t when t == typeof(CollisionNode)     => ClassPool.Get<ClientCollisionNode>().Initialize(characterBehaviour, nodeBase),
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
        public void OnRent() { }

        public void OnReturn() {
            Stop();
            _info      = null;
            _character = null;

            foreach (var node in _nodes) {
                node.Release();
                ClassPool.Release(node);
            }

            _nodesByGuid.Clear();

            _nodes       = Array.Empty<ClientNodeBase>();
            _nodesByGuid = null;
            _currentNode = null;
        }
    }
}