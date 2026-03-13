using System;
using System.Collections.Generic;
using Script.GamePlay.Character.Interface;
using Script.GameInfo.Info.Character;
using Script.Utility.Runtime;

namespace Script.GamePlay.Character {
    
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