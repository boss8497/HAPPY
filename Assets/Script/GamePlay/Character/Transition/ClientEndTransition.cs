using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientEndTransition : ClientTransitionBase {
        public override bool OnTrigger() {
            return true;
        }
    }
}