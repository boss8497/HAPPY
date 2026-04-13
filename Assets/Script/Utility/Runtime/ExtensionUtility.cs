using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Script.Utility.Runtime {
    public static class ExtensionUtility {
        public static void SetActiveSafe(this GameObject obj, bool active) {
            if (obj == null || obj.activeSelf == active) return;
            obj.SetActive(active);
        }

        public static void ClickAddListener(this Button btn, UnityAction listener, bool removeAll = true) {
            if (btn == null || listener == null) return;
            if (removeAll) {
                btn.onClick.RemoveAllListeners();
            }

            btn.onClick.AddListener(listener);
        }
    }
}