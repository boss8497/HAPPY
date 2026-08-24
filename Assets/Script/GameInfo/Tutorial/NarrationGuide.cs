using Script.GameInfo.Attribute;
using UnityEngine;

namespace Script.GameInfo.Info {
    public class NarrationGuide : GuideBase {
        public string name      = string.Empty;
        
        [TextArea]
        public string guideText = string.Empty;
        
        [AssetPath(typeof(Sprite))]
        public string iconPath = string.Empty;
    }
}