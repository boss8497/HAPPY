using Script.GameInfo.Enum;

namespace Script.GameInfo.Dungeon {
    [System.Serializable]
    public abstract class ActionBase {
        public SerializeGuid guid = SerializeGuid.NewGuid();
        public EventTiming   timing;
    }
}