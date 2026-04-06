using Script.GameInfo.Attribute;
using UnityEngine;

namespace Script.GameInfo.Dungeon {
    [System.Serializable]
    public class EnemySpawnAction : ActionBase {
        [Character]
        public int     uid;
        public Vector3 position;
    }
}