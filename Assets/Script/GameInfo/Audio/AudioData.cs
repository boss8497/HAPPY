using Script.GameInfo.Attribute;
using UnityEngine;

namespace Script.GameInfo.Info {
    [System.Serializable]
    public class AudioData {
        [AssetPath(typeof(Object))]
        public string key;

        public AudioGroup type;
        public bool           loop;
        public bool           autoRelease = true;
        public float          pitch       = 1f;
        public bool           is3D;
    }
}