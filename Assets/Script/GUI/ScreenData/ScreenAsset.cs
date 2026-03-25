using Script.GUI.Enum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Script.GUI {
    [System.Serializable]
    public struct ScreenAsset {
        public string                      id;
        public AssetReferenceT<GameObject> screen;
        public ScreenType                  type;
    }
}