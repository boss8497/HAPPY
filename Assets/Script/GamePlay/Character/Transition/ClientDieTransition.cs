using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    public class ClientDieTransition : ClientTransitionBase {
        public override bool OnTrigger() {
            if (Character?.Die == null) return false;
            return Value == Character.Die.CurrentValue;
        }
    }
}