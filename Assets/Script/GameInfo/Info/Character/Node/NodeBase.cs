using System;
using Script.GameInfo.Info.Character.Transition;

namespace Script.GameInfo.Info.Character.Node {
    public class NodeBase {
        public Guid   Guid = Guid.NewGuid();
        public string Id;
        
        public TransitionBase[] Transitions;
    }
}