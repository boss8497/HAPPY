using System;
using System.Collections.Generic;
using Script.GameInfo.Info.Character.Behaviour;
using Script.GamePlay.Character.Interface;
using Script.GamePlay.Character.Node;
using Script.Utility.Runtime;

namespace Script.GamePlay.Character.Behaviour {
    
    [Serializable]
    public class CharacterBehaviour : IClassPool {
        private BehaviourInfo _info;
        private ICharacter    _character;

        private Dictionary<Guid, ClientNodeBase> _nodes;
        
        public void Initialize(BehaviourInfo info, ICharacter character) {
            _info      = info;
            _character = character;
        }

        
        
        //<summary> Called when the character is rented from the pool. </summary>
        public void OnRent() {
        }
        public void OnReturn() {
        }
    }
}