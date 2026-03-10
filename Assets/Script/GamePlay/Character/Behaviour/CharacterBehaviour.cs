using System;
using System.Collections.Generic;
using Script.GameInfo.Info.Character.Behaviour;
using Script.GamePlay.Character.Interface;
using Script.GamePlay.Character.Node;

namespace Script.GamePlay.Character.Behaviour {
    
    [Serializable]
    public class CharacterBehaviour {
        private BehaviourInfo _info;
        private ICharacter    _character;

        private Dictionary<Guid, ClientNodeBase> _nodes;
        
        public void Initialize(BehaviourInfo info, ICharacter character) {
            _info      = info;
            _character = character;
        }
    }
}