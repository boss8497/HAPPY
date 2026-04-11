using UnityEngine;

namespace Script.Utility.Runtime {
    public static class ExtensionUtility {
        public static void SetActiveSafe(this GameObject obj, bool active) {
            if (obj == null || obj.activeSelf == active) return;
            obj.SetActive(active);
        }
    }
}