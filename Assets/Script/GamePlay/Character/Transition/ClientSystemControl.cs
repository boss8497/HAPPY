using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientSystemControl : ClientTransitionBase {
        private Character _character;

        public override ClientTransitionBase Initialize(ClientNodeBase node, TransitionBase transitionBase) {
            _character = node.CharacterBehaviour.Character;
            return base.Initialize(node, transitionBase);
        }

        public override bool OnTrigger() {
            if (_character?.SystemControl == null) return false;
            return _character.SystemControl.CurrentValue == Value;
        }
    }
}