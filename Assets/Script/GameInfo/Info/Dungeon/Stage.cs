using System;
using Script.GameInfo.Attribute;
using UnityEngine.SceneManagement;

namespace Script.GameInfo.Dungeon {
    [AutoEditorTable(true)]
    [Serializable]
    public class Stage {
        public SerializeGuid guid = SerializeGuid.NewGuid();
        public string        GuidString => guid.ToString();

        public string id;

        [LocalizePath("@\"Stage/Name/\" + GuidString", false)]
        public string name;

        [LocalizePath("@\"Stage/Description/\" + GuidString", false)]
        public string description;

        [Phase]
        public int[] phaseInfos;


        [AssetPath(typeof(Scene))]
        public string scenePath = string.Empty;
    }
}