using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Script.Utility.Runtime {
    public static class ExtensionUtility {
        public static void SetActiveSafe(this GameObject obj, bool active) {
            if (obj == null || obj.activeSelf == active) return;
            obj.SetActive(active);
        }

        public static void SetActiveSafe(this Component component, bool active) {
            var obj = component?.gameObject;
            if (obj == null || obj.activeSelf == active) return;
            obj.SetActive(active);
        }

        public static void ClickAddListener(this Button btn, UnityAction listener, bool removeAll = true) {
            if (removeAll) {
                btn.onClick.RemoveAllListeners();
            }
            
            if (btn == null || listener == null) return;
            btn.onClick.AddListener(listener);
        }
        public static void AddListener(this UnityEvent unityEvent, UnityAction listener, bool removeAll = true) {
            if (removeAll) {
                unityEvent.RemoveAllListeners();
            }
            
            if (unityEvent == null || listener == null) return;
            unityEvent.AddListener(listener);
        }
    }
}