using Script.GameInfo.Info.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientPlayerControl : ClientTransitionBase{
        public ClientPlayerControl(ClientNodeBase node, TransitionBase transitionBase) : base(node, transitionBase) { }

        public override bool OnTrigger() {
            return false;
        }
    }
}