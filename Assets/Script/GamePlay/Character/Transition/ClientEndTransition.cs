using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientEndTransition : ClientTransitionBase {
        public ClientEndTransition(ClientNodeBase node, TransitionBase transitionBase) : base(node, transitionBase) { }

        public override bool OnTrigger() {
            return true;
        }
    }
}