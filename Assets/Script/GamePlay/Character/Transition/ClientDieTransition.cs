using Script.GameInfo.Info.Character;

namespace Script.GamePlay.Character {
    public class ClientDieTransition : ClientTransitionBase {
        public ClientDieTransition(ClientNodeBase node, TransitionBase transitionBase) : base(node, transitionBase) { }
        public override bool OnTrigger() {
            return Value && (Character?.Die ?? false);
        }
    }
}