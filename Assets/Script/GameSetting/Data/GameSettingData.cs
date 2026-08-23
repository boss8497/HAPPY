using Script.GameInfo.Attribute;
using UnityEngine.SceneManagement;

namespace Script.GameSetting.Data {
    [System.Serializable]
    public struct GameSettingData {
        public int frameRate;
        public int vSyncCount;

        public float addressableCacheCheckIntervalSeconds;
        public float addressableCacheReleaseGraceSeconds;
    }
}