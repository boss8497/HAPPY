using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info.Character;

namespace Script.GamePlay.Character {
    [Serializable]
    public abstract class ClientNodeBase {
        private static readonly int DefaultDelayFrame = 4;
        
        private CharacterBehaviour _characterBehaviour;
        private NodeBase           _nodeBase;

        protected Dictionary<TransitionTiming, ClientTransitionBase[]> _transitionBases;

        protected bool              isPlay;
        protected CancellationToken playCts;

        //public
        public CharacterBehaviour CharacterBehaviour => _characterBehaviour;
        public NodeBase           NodeBase           => _nodeBase;
        public bool               IsPlay             => isPlay;

        protected ClientNodeBase(CharacterBehaviour characterBehaviour, NodeBase nodeBase) {
            _characterBehaviour = characterBehaviour;
            _nodeBase           = nodeBase;

            _transitionBases = _nodeBase.transitions.GroupBy(t => t.timing)
                                        .ToDictionary(g => g.Key, g => g.Select(Create).ToArray());
        }

        private static ClientTransitionBase Create(TransitionBase transitionBase) {
            var type = transitionBase.GetType();

            return type switch {
                var t when t == typeof(PlayerControl) => new ClientPlayerControl(),
                var t when t == typeof(SystemControl) => new ClientSystemControl(),
                _                                     => null
            };
        }
        
        
        //실행 순서 Initialize -> Enter -> Start -> End
        public abstract void Initialize();

        protected virtual void Enter() {
        }

        public UniTask Start(CancellationToken cts = default) {
            playCts = cts;
            UpdateTransition(playCts).Forget();
            Update(playCts).Forget();

            return UniTask.CompletedTask;
        }

        private async UniTask UpdateTransition(CancellationToken cts) {
            while (!cts.IsCancellationRequested) {
                var transition = CheckTransition(TransitionTiming.Update);
                if (transition != null) {
                    break;
                }

                await UniTask.DelayFrame(DefaultDelayFrame, cancellationToken: cts);
            }
        }

        private ClientTransitionBase CheckTransition(TransitionTiming timing) {
            if (_transitionBases.TryGetValue(TransitionTiming.Update, out var clientTransitions) == false) {
                return null;
            }

            return clientTransitions.FirstOrDefault(r => r.OnTrigger());
        }

        protected abstract UniTask Update(CancellationToken cts);
        
        protected virtual void End() {
        }
    }
}