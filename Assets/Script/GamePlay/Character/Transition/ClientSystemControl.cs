using Script.GameInfo.Info.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientSystemControl : ClientTransitionBase {
        
        
        public override void Initialize(ClientNodeBase node, TransitionBase transitionBase) {
            base.Initialize(node, transitionBase);
        }

        public override bool OnTrigger() {
            throw new System.NotImplementedException();
        }
    }
}