using Script.GameInfo.Attribute;
using UnityEngine.SceneManagement;

namespace Script.GameSetting.Data {
    [System.Serializable]
    public struct GameSettingData {
        public int frameRate;
        public int vSyncCount;

        [AssetPath(typeof(Scene))]
        public string startUpScenePath;

        [AssetPath(typeof(Scene))]
        public string titleScenePath;
    }
}