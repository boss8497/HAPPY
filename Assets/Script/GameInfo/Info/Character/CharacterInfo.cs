using System;
using Script.GameInfo.Base;
using Expression;
using Script.GameInfo.Attribute;
using UnityEngine;
using AnimationEvent = Script.GameInfo.Info.Character.Animation.AnimationEvent;

namespace Script.GameInfo.Info.Character {
    [System.Serializable]
    public class CharacterInfo : InfoBase {
        [field: SerializeField]
        public Expression.Expression Test = new("1 + 2 * 3");
        
        [SerializeReference]
        public AnimationEvent[] animationEvents = Array.Empty<AnimationEvent>();
        
        [Behaviour]
        public int behaviourId;
    }
}