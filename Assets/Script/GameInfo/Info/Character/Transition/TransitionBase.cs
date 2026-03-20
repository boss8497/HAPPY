using Script.GameInfo.Attribute;
using Script.GameInfo.Enum;

namespace Script.GameInfo.Character {
    [System.Serializable]
    public abstract class TransitionBase {
        public SerializeGuid guid = SerializeGuid.NewGuid();
        public string        id;
        public EventTiming   timing;

        //비교 값
        public bool value;

        //우선 순위
        public byte priority;

        [NextNode]
        public SerializeGuid nextNodeGuid;
    }
}