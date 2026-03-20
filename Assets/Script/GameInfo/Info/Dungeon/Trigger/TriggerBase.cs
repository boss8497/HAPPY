namespace Script.GameInfo.Dungeon {
    public enum TriggerType {
        StageFail,
        StageClear,
    }
    
    
    [System.Serializable]
    public abstract class TriggerBase {
        public TriggerType type;
    }
}