using System;
using Script.GameInfo.Base;
using Script.GameInfo.Attribute;
using UnityEngine;

namespace Script.GameInfo.Character {
    [System.Serializable]
    public class CharacterInfo : InfoBase {
        [SerializeReference]
        public AnimationEvent[] animationEvents = Array.Empty<AnimationEvent>();
        
        [Behaviour]
        public int behaviourId;

        [Status]
        public int[] statusUids = Array.Empty<int>();
    }
}