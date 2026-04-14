using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.GameInfo.Enum;

namespace Script.GamePlay.Character {
    [Serializable]
    public abstract class ClientNodeBase {
        protected static readonly int DefaultDelayFrame = 4;

        protected CharacterBehaviour _characterBehaviour;
        protected NodeBase           _nodeBase;

        protected Dictionary<EventTiming, ClientTransitionBase[]> _transitionBases;

        protected long              nodeGeneration;
        protected CancellationToken playCts;

        //public
        public CharacterBehaviour CharacterBehaviour => _characterBehaviour;
        public NodeBase           NodeBase           => _nodeBase;
        public bool               IsPlay             => playCts.IsCancellationRequested == false;

        protected ClientNodeBase(CharacterBehaviour characterBehaviour, NodeBase nodeBase) {
            _characterBehaviour = characterBehaviour;
            _nodeBase           = nodeBase;

            _transitionBases = _nodeBase.transitions.GroupBy(t => t.timing)
                                        .ToDictionary(g => g.Key, g => g.Select(s => Create(this, s))
                                                                        .OrderByDescending(o => o.Priority)
                                                                        .ToArray());
        }

        private static ClientTransitionBase Create(ClientNodeBase nodeBase, TransitionBase transitionBase) {
            var type = transitionBase.GetType();

            return type switch {
                var t when t == typeof(PlayerControl) => new ClientPlayerControl(nodeBase, transitionBase),
                var t when t == typeof(SystemControl) => new ClientSystemControl(nodeBase, transitionBase),
                var t when t == typeof(EndTransition) => new ClientEndTransition(nodeBase, transitionBase),
                var t when t == typeof(DieTransition) => new ClientDieTransition(nodeBase, transitionBase),
                _                                     => null
            };
        }


        //실행 순서 Initialize -> Enter -> Start -> End
        public abstract void Initialize();

        protected virtual void Enter() { }

        public async UniTask Start(CancellationToken cts = default) {
            var currentGeneration = ++nodeGeneration;

            var beginTransition = CheckTransition(EventTiming.Begin);
            if (beginTransition != null) {
                _characterBehaviour.OnTransition(this, beginTransition).Forget();
                return;
            }

            Enter();

            playCts = cts;
            UpdateTransition(currentGeneration, playCts).Forget();


            await Update(playCts);
            if (!cts.IsCancellationRequested && currentGeneration == nodeGeneration) {
                var endTransition = CheckTransition(EventTiming.End);
                if (endTransition != null) {
                    _characterBehaviour.OnTransition(this, endTransition).Forget();
                }
            }
        }

        public virtual void Stop() {
            End();
        }

        private async UniTask<ClientTransitionBase> UpdateTransition(long generation, CancellationToken cts) {
            while (!cts.IsCancellationRequested && generation == nodeGeneration) {
                var transition = CheckTransition(EventTiming.Update);
                if (transition != null && generation == nodeGeneration) {
                    _characterBehaviour.OnTransition(this, transition).Forget();
                    return transition;
                }

                var isCancel = await UniTask.Yield(cancellationToken: cts)
                                            .SuppressCancellationThrow();

                if (isCancel) break;
            }

            return null;
        }

        private ClientTransitionBase CheckTransition(EventTiming timing) {
            if (_transitionBases.TryGetValue(timing, out var clientTransitions) == false) {
                return null;
            }

            return clientTransitions.FirstOrDefault(r => r.OnTrigger());
        }

        protected abstract UniTask Update(CancellationToken cts);

        protected virtual void End() { }
    }
}