using Script.GameInfo.Base;
using Script.GameInfo.Info.Character.Node;
using UnityEngine;

namespace Script.GameInfo.Info.Character {
    [System.Serializable]
    public class BehaviourInfo : InfoBase {
        [SerializeReference]
        public NodeBase[] nodes;
    }
}