using Script.GUI.Screen.Enum;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Script.GUI.Screen {
    [System.Serializable]
    public struct ScreenAsset {
        public string                      id;
        public AssetReferenceT<GameObject> screen;
    }
}