using System;

namespace Script.GameInfo.Info.Character.Transition {
    public enum TransitionTiming {
        Being,
        Update,
        End,
    }
    
    public class TransitionBase {
        public Guid   Guid = Guid.NewGuid();
        public string Id;
        public TransitionTiming Timing;
        
        public Guid ToNodeGuid;
    }
}