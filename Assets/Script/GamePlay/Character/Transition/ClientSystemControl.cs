using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientSystemControl : ClientTransitionBase {
        public ClientSystemControl(ClientNodeBase node, TransitionBase transitionBase) : base(node, transitionBase) { }

        public override bool OnTrigger() {
            return false;
        }
    }
}