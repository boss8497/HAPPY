using Script.GameInfo.Attribute;

namespace Script.GameInfo.Info.Character {
    public enum TransitionTiming {
        Being,
        Update,
        End,
    }
    
    [System.Serializable]
    public abstract class TransitionBase {
        public SerializeGuid    guid = SerializeGuid.NewGuid();
        public string           id;
        public TransitionTiming timing;

        //비교 값
        public bool value;
        //우선 순위
        public byte priority;
        
        [NextNode]
        public SerializeGuid nextNodeGuid;
    }
}