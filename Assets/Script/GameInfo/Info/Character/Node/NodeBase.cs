using Script.GameInfo.Info.Character.Transition;
using UnityEngine;

namespace Script.GameInfo.Info.Character.Node {
    [System.Serializable]
    public abstract class NodeBase {
        public SerializeGuid guid = SerializeGuid.NewGuid();
        public string        id;
        
        [SerializeReference]
        public TransitionBase[] transitions;
    }
}