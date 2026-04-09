namespace Script.GameInfo.Dungeon {
    public enum TriggerType {
        Fail,
        Clear,
    }
    
    
    [System.Serializable]
    public abstract class TriggerBase {
        public TriggerType type;
    }
}