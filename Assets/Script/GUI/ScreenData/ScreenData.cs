using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Script.GUI {
    [System.Serializable]
    public class ScreenData : ScriptableObject {
        public ScreenAsset[] screens;
    }
}