using System;
using Script.GameInfo.Info.Character.Transition;
using UnityEngine;

namespace Script.GameInfo.Info.Character.Node {
    [System.Serializable]
    public abstract class NodeBase {
        public Guid   Guid = Guid.NewGuid();
        public string id;
        
        [SerializeReference]
        public TransitionBase[] transitions;
    }
}