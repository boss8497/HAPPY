using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    public class ClientCollisionTransition : ClientTransitionBase {
        public override bool OnTrigger() {
            if (Character?.CollisionState == null) return false;
            return Value == Character.CollisionState.CurrentValue;
        }
    }
}