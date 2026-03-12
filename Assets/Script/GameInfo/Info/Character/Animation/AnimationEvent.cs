using System;

namespace Script.GameInfo.Info.Character.Animation {
    [System.Serializable]
    public abstract class AnimationEvent {
        public SerializeGuid guid = SerializeGuid.NewGuid();
        public string        animationName;
        public float         duration;
        public float         time;
    }
}