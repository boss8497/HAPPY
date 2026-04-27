using System;
using UnityEngine;

namespace Script.GameInfo.Character {
    [System.Serializable]
    public class CharacterHitbox {
        public CharacterState state;
        public Hitbox         hitbox = new();
    }
}