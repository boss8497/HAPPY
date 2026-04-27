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

        // 기본 Invisible로 생성 해준다
        // Invisible이면 쿼리에 등록은 되지만 Continue
        [SerializeReference]
        public Hitbox hitbox = new();
        
        // 상태 변화에 따라 Hitbox를 다르게 해줄 의도
        public CharacterHitbox[] hitboxes = Array.Empty<CharacterHitbox>();
    }
}