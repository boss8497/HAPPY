using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Script.GameInfo.Character {
    [Serializable]
    public class Hitbox {
        public HitBoxType type;
        public Vector3    offset;

        [ShowIf("@type == HitBoxType.Rect")]
        public Vector3 size;

        [ShowIf("@type == HitBoxType.Circle")]
        public float radius;
    }
}