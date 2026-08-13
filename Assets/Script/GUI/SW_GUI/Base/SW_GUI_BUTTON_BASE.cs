using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SW.GUI.Base {
    public abstract class SW_GUI_BUTTON_BASE : SW_GUI_BASE, IPointerClickHandler {
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

        #endregion
        
        
        
        
        private UnityEvent _scriptClickEvent = new();
        private float _nextDelayTime;

        public abstract override void Initialize();

        protected virtual void InteractableChanged() {
        }
        
        public void OnPointerClick(PointerEventData eventData) {
            Click();
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

        public abstract void OnClick();
        


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