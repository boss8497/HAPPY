using System;

namespace Script.GameInfo.Character {
    [System.Serializable]
    public abstract class AnimationEvent {
        public SerializeGuid guid = SerializeGuid.NewGuid();
        public string        animationName;
        public float         duration;
        public float         time;
    }
}