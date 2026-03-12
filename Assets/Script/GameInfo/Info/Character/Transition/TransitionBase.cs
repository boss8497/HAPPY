using System;
using Script.GameInfo.Attribute;

namespace Script.GameInfo.Info.Character.Transition {
    public enum TransitionTiming {
        Being,
        Update,
        End,
    }
    
    [System.Serializable]
    public abstract class TransitionBase {
        public Guid   Guid = Guid.NewGuid();
        public string id;
        public TransitionTiming timing;
        
        [NextNode]
        public SerializeGuid nextNodeGuid;
    }
}