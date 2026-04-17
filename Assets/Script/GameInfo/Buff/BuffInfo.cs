using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using UnityEngine;

namespace Script.GameInfo.Info {
    [AutoEditorTable(true)]
    [System.Serializable]
    public class BuffInfo : InfoBase {
        [Status]
        public int statusUid;
        public float time;

        [AssetPath(typeof(Sprite))]
        public string icon;
    }
}