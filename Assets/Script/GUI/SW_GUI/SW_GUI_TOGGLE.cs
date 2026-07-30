using SW.GUI.Base;
using UnityEngine;
using UnityEngine.Events;

namespace SW.GUI {
    /// <summary>
    /// 체크마크(GameObject)를 On/Off 상태에 맞춰 활성/비활성 처리하는 기본 Toggle 구현
    /// </summary>
    public class SW_GUI_TOGGLE : SW_GUI_TOGGLE_BASE {
        #region Inspector

        [SerializeField]
        private GameObject _checkmark;

        [SerializeField]
        private UnityEvent onToggleOn = new();

        [SerializeField]
        private UnityEvent onToggleOff = new();

        #endregion

        public override void Initialize() {
            ApplyVisual();
        }

        protected override void OnToggleOnEvent() {
            ApplyVisual();
            onToggleOn?.Invoke();
        }

        protected override void OnToggleOffEvent() {
            ApplyVisual();
            onToggleOff?.Invoke();
        }

        private void ApplyVisual() {
            if (_checkmark != null) {
                _checkmark.SetActive(_isOn);
            }
        }
    }
}
