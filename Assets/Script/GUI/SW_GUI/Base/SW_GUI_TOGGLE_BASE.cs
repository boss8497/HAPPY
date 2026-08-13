using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace SW.GUI.Base {
    /// <summary>
    /// On/Off 상태를 가지는 Toggle Base Class
    /// 클릭 시 상태가 반전되고, 상태가 실제로 바뀔 때만 비주얼/이벤트가 갱신된다.
    /// </summary>
    public abstract class SW_GUI_TOGGLE_BASE : SW_GUI_BUTTON_BASE {
        [Serializable]
        public class ToggleEvent : UnityEvent<bool> { }

        #region Inspector

        [SerializeField]
        [OnValueChanged("IsOnInspectorChanged")]
        protected bool _isOn = false;

        [SerializeField]
        private ToggleEvent onValueChanged = new();

        #endregion

        private ToggleEvent _scriptValueChangedEvent = new();

        public bool IsOn {
            get => _isOn;
            set => SetIsOn(value, true);
        }

        public abstract override void Initialize();

        public override void OnClick() {
            SetIsOn(!_isOn, true);
        }

        /// <summary>
        /// 코드에서 On/Off 상태를 변경할 때 사용.
        /// notify가 false면 이벤트 호출 없이 상태 + 비주얼만 갱신한다. (데이터 로드 시 초기값 세팅 등)
        /// </summary>
        public void SetIsOn(bool isOn, bool notify) {
            _isOn = isOn;
            InvokeToggleEvent(_isOn, notify);

            if (notify) {
                _scriptValueChangedEvent?.Invoke(_isOn);
                // Inspector Event Call을 제일 마지막에 해준다.
                onValueChanged?.Invoke(_isOn);
            }
        }

        public void SetIsOnWithoutNotify(bool isOn) {
            SetIsOn(isOn, false);
        }

        public UnityEvent<bool> AddValueChangedListener(UnityAction<bool> listener, bool removeAll = true) {
            _scriptValueChangedEvent ??= new();
            if (listener == null) return _scriptValueChangedEvent;
            if (removeAll) {
                _scriptValueChangedEvent.RemoveAllListeners();
            }

            _scriptValueChangedEvent.AddListener(listener);
            return _scriptValueChangedEvent;
        }

        private void InvokeToggleEvent(bool isOn, bool notify = true) {
            if (notify == false) return;

            if (isOn) {
                OnToggleOnEvent();
            }
            else {
                OnToggleOffEvent();
            }
        }

        protected abstract void OnToggleOnEvent();
        protected abstract void OnToggleOffEvent();

        #region Odin

        private void IsOnInspectorChanged() {
            InvokeToggleEvent(_isOn);
        }

        #endregion
    }
}