using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SW.GUI.Base {
    public abstract class SW_GUI_BUTTON_BASE : SW_GUI_BASE, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler {
        #region Inspector
        
        [ToggleGroup("_interactable", "버튼 사용 여부")]
        [SerializeField]
        [OnValueChanged("InteractableChanged")]
        protected bool _interactable = true;
        public bool Interactable {
            get => _interactable;
            set {
                _interactable = value;
                InteractableChanged();
            }
        }

        [ToggleGroup("useDelay", "클릭 딜레이")]
        [SerializeField]
        private bool useDelay = false;

        [ToggleGroup("useDelay"), LabelText("시간")]
        [SerializeField]
        private float delay = 0.05f;

        [SerializeField]
        private UnityEvent onClickEvent = new();

        [SerializeField]
        private UnityEvent onPressEvent = new();

        [SerializeField]
        private UnityEvent onReleaseEvent = new();

        #endregion


        public bool IsPressed { get; private set; }


        private UnityEvent _scriptClickEvent = new();
        private UnityEvent _scriptPressEvent = new();
        private UnityEvent _scriptReleaseEvent = new();
        private float _nextDelayTime;

        public abstract override void Initialize();

        protected virtual void InteractableChanged() {
            // 눌린 상태에서 비활성화되면 Held 상태로 고정되지 않도록 강제로 뗀다.
            if (_interactable == false && IsPressed) {
                Release();
            }
        }

        protected virtual void OnDisable() {
            // 오브젝트가 비활성화/파괴될 때 눌린 상태가 남아있으면 강제로 뗀다 (모바일에서 손가락 뗌 이벤트를 못 받는 경우 대비).
            if (IsPressed) {
                Release();
            }
        }

        public void OnPointerClick(PointerEventData eventData) {
            Click();
        }

        public void OnPointerDown(PointerEventData eventData) {
            Press();
        }

        public void OnPointerUp(PointerEventData eventData) {
            Release();
        }

        public void Click() {
            if (_interactable == false) return;

            var currentTime = Time.unscaledTime;
            if (useDelay && currentTime < _nextDelayTime) {
                return;
            }

            _scriptClickEvent?.Invoke();
            OnClick();

            // Inspector Event Call을 제일 마지막에 해준다.
            onClickEvent?.Invoke();


            // Delay를 사용하는 경우, 다음 클릭 가능 시간을 갱신한다.
            if (useDelay) {
                _nextDelayTime = currentTime + delay;
            }
        }

        public void Press() {
            if (_interactable == false || IsPressed) return;

            IsPressed = true;

            _scriptPressEvent?.Invoke();
            OnPress();

            // Inspector Event Call을 제일 마지막에 해준다.
            onPressEvent?.Invoke();
        }

        public void Release() {
            if (IsPressed == false) return;

            IsPressed = false;

            _scriptReleaseEvent?.Invoke();
            OnRelease();

            // Inspector Event Call을 제일 마지막에 해준다.
            onReleaseEvent?.Invoke();
        }

        public abstract void OnClick();

        protected virtual void OnPress() {
        }

        protected virtual void OnRelease() {
        }



        public UnityEvent AddClickListener(UnityAction listener, bool removeAll = true) {
            _scriptClickEvent ??= new();
            if (listener == null) return _scriptClickEvent;
            if (removeAll) {
                _scriptClickEvent.RemoveAllListeners();
            }

            _scriptClickEvent.AddListener(listener);
            return _scriptClickEvent;
        }
        
        public void RemoveClickListener(UnityAction listener) {
            if (_scriptClickEvent == null || listener == null) return;
            _scriptClickEvent.RemoveListener(listener);
        }

        public UnityEvent AddPressListener(UnityAction listener, bool removeAll = true) {
            _scriptPressEvent ??= new();
            if (listener == null) return _scriptPressEvent;
            if (removeAll) {
                _scriptPressEvent.RemoveAllListeners();
            }

            _scriptPressEvent.AddListener(listener);
            return _scriptPressEvent;
        }

        public void RemovePressListener(UnityAction listener) {
            if (_scriptPressEvent == null || listener == null) return;
            _scriptPressEvent.RemoveListener(listener);
        }

        public UnityEvent AddReleaseListener(UnityAction listener, bool removeAll = true) {
            _scriptReleaseEvent ??= new();
            if (listener == null) return _scriptReleaseEvent;
            if (removeAll) {
                _scriptReleaseEvent.RemoveAllListeners();
            }

            _scriptReleaseEvent.AddListener(listener);
            return _scriptReleaseEvent;
        }

        public void RemoveReleaseListener(UnityAction listener) {
            if (_scriptReleaseEvent == null || listener == null) return;
            _scriptReleaseEvent.RemoveListener(listener);
        }

        public UnityEvent AddListener(UnityEvent uniEvent, UnityAction listener, bool removeAll = true) {
            uniEvent ??= new();
            if (listener == null) return uniEvent;
            if (removeAll) {
                uniEvent.RemoveAllListeners();
            }

            uniEvent.AddListener(listener);
            return uniEvent;
        }
    }
}