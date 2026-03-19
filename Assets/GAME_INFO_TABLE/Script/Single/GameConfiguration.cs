using Script.GameInfo.Info;
using UnityEngine;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "GameConfiguration", menuName = "Data/Single/GameConfiguration")]
    public class GameConfiguration : ScriptableObject {
        public ConfigurationInfo config;
    }
}