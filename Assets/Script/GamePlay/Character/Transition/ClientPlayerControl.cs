using Script.GameInfo.Character;
using Script.GamePlay.Input;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientPlayerControl : ClientTransitionBase{
        private IPlayerControls  _controls;

        public ClientPlayerControl(ClientNodeBase node, TransitionBase transitionBase) : base(node, transitionBase) {
            _controls = node?.CharacterBehaviour?.Character?.PlayerControls;
        }

        public override bool OnTrigger() {
            if (_controls == null) return false;
            return _controls.HasAnyInput && Value;
        }
    }
}