using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientSystemControl : ClientTransitionBase {
        private Character _character;

        public ClientSystemControl(ClientNodeBase node, TransitionBase transitionBase) : base(node, transitionBase) {
            _character = node.CharacterBehaviour.Character;
        }

        public override bool OnTrigger() {
            if (_character?.SystemControl == null) return false;
            return _character.SystemControl.CurrentValue == Value;
        }
    }
}