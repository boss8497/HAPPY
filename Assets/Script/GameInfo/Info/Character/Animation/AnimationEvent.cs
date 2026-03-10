using System;

namespace Script.GameInfo.Info.Character.Animation {
    [System.Serializable]
    public abstract class AnimationEvent {
        public Guid   Guid = Guid.NewGuid();
        public string AnimationName;
        public float  Duration;
        public float  Time;
    }
}