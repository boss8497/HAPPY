using Script.GameInfo.Attribute;
using UnityEngine;

namespace Script.GameInfo.Dungeon {
    [System.Serializable]
    public class EnemySpawnAction : ActionBase {
        public SpawnData[] spawnData;
    }
    
    [System.Serializable]
    public class SpawnData {
        [Character]
        public int     uid;
        public Vector3 position;
    }
}