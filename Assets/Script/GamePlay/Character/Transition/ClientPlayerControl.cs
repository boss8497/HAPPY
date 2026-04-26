using Script.GameInfo.Character;
using Script.GamePlay.Input;
using UnityEngine;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public class ClientPlayerControl : ClientTransitionBase{
        private IPlayerControls  _controls;
        private Character _character;

        public ClientPlayerControl(ClientNodeBase node, TransitionBase transitionBase) : base(node, transitionBase) {
            _character = node?.CharacterBehaviour?.Character; 
            _controls  = _character?.PlayerControls;
        }

        public override bool OnTrigger() {
            if (_controls == null || (_character?.IsPlayer ?? false) == false) return false;
            return _controls.HasAnyInput == Value;
        }
    }
}