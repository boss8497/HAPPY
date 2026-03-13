using UnityEngine;

namespace Script.GameInfo.Info.Character {
    [System.Serializable]
    public abstract class NodeBase {
        public SerializeGuid guid = SerializeGuid.NewGuid();
        public string        id;

        [SerializeReference]
        public TransitionBase[] transitions;
    }
}