using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    public class ClientDieTransition : ClientTransitionBase {
        public ClientDieTransition(ClientNodeBase node, TransitionBase transitionBase) : base(node, transitionBase) { }
        public override bool OnTrigger() {
            if (Character?.Die == null) return false;
            return Value == Character.Die.CurrentValue;
        }
    }
}