using UnityEngine;

namespace SW.GUI.Base {
    /// <summary>
    /// 모든 GUI 기능 (Button, Toggle, Slide 등..) Base Class
    /// </summary>
    public abstract class SW_GUI_BASE : MonoBehaviour {
        /// <summary>
        /// Initialize는 사용자가 직접 호출해주는 방식으로 설계
        /// </summary>
        /// <returns></returns>
        public abstract void Initialize();
    }
}