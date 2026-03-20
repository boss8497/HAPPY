using Script.GameInfo.Base;
using UnityEngine;

namespace Script.GameInfo.Character {
    [System.Serializable]
    public class BehaviourInfo : InfoBase {
        [SerializeReference]
        public NodeBase[] nodes;
    }
}