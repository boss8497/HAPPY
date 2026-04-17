using System;
using Script.GameInfo.Base;
using Script.GameInfo.Attribute;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Script.GameInfo.Character {
    [System.Serializable]
    public class CharacterInfo : InfoBase {
        public CharacterType type;
        
        [SerializeReference]
        public AnimationEvent[] animationEvents = Array.Empty<AnimationEvent>();

        [Behaviour]
        public int behaviourId;

        [Status]
        public int[] statusUids = Array.Empty<int>();

        [ShowIf("@type == CharacterType.Buff"), Buff]
        public int[] buffUids = Array.Empty<int>();

        [AssetPath(typeof(GameObject))]
        public string prefab;

        [SerializeReference]
        public Hitbox            hitbox;
        public CharacterHitbox[] hitboxes = Array.Empty<CharacterHitbox>();
    }
}