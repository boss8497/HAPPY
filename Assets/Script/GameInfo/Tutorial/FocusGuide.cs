using Newtonsoft.Json;
using Script.GameInfo.Attribute;
using UnityEngine;

namespace Script.GameInfo.Info {
    [System.Serializable]
    public class FocusGuide : GuideBase {
        [Focus]
        public SerializeGuid focusGuid;
        public bool          flip;

        public string name      = string.Empty;
        public string guideText = string.Empty;

        [AssetPath(typeof(Sprite))]
        public string iconPath = string.Empty;
    }
}