using UnityEngine;

namespace Script.GUI.Screen {
    public partial class ScreenManager {
        public GameObject PoolPop(string key, Transform parent = null, bool active = true) {
            return _uiPooling.Pop(key, parent, active);
        }

        public bool PoolPush(GameObject obj) {
            return _uiPooling.Push(obj);
        }
    }
}