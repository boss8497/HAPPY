using Script.GameInfo.Info;
using Script.GameInfo.Info.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public abstract class ClientTransitionBase {
        private ClientNodeBase _node;
        private TransitionBase _transitionBase;

        public bool          Value        => _transitionBase?.value ?? false;
        public SerializeGuid NextNodeGuid => _transitionBase.nextNodeGuid;
        public int           Priority     => _transitionBase?.priority ?? 0;

        public ClientTransitionBase(ClientNodeBase node, TransitionBase transitionBase) {
            _node           = node;
            _transitionBase = transitionBase;
        }

        public virtual void Release() { }


        public abstract bool OnTrigger();
    }
}