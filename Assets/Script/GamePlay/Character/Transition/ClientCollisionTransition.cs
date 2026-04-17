using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    public class ClientCollisionTransition : ClientTransitionBase {
        public ClientCollisionTransition(ClientNodeBase node, TransitionBase transitionBase) : base(node, transitionBase) {
        }
        public override bool OnTrigger() {
            if (Character?.CollisionState == null) return false;
            return Value == Character.CollisionState.CurrentValue;
        }
    }
}