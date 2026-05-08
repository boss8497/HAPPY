using Script.GameInfo.Attribute;
using UnityEngine;

namespace Script.GameInfo.Dungeon {
    [System.Serializable]
    public class EnemySpawnArrayAction : ActionBase {
        [Character]
        public int uid;

        public Vector3[] positions;
    }
}