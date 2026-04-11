using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Script.GUI.Screen {
    [System.Serializable]
    public struct ScreenAsset {
        [ReadOnly]
        public string id;

        [OnValueChanged("ScreenValueChanged", true)]
        public AssetReferenceT<GameObject> screen;


        private void ScreenValueChanged() {
#if UNITY_EDITOR
            if (screen == null) {
                id = string.Empty;
                return;
            }

            var screenScript = screen.editorAsset.GetComponent<Screen>();
            if (screenScript == null) {
                Debug.LogError($"ScreenAsset의 screen 필드에 할당된 에셋은 Screen 컴포넌트를 포함해야 합니다. 현재 할당된 에셋: {screen.editorAsset.name}");
            }

            id = screenScript.Key;
#endif
        }
    }
}