using System;
using UnityEngine;

namespace Script.GameInfo.Character {
    [System.Serializable]
    public class CharacterHitbox {
        public CharacterState state;

        [SerializeReference]
        public Hitbox[] hitboxes = Array.Empty<Hitbox>();
    }
}