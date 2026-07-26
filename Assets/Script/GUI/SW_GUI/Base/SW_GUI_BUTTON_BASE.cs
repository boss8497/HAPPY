using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SW.GUI.Base {
    public abstract class SW_GUI_BUTTON_BASE : SW_GUI_BASE, IPointerClickHandler {
        [SerializeField]
        private UnityEvent onClickEvent = new();
        
        private UnityEvent scriptClickEvent = new();

        public abstract override void Initialize();
        
        public void OnPointerClick(PointerEventData eventData) {
            scriptClickEvent?.Invoke();
            OnClick();
            
            // Inspector Event Call을 제일 마지막에 해준다.
            onClickEvent?.Invoke();
        }

        public abstract void OnClick();
        
        
        public UnityEvent AddClickListener(UnityAction listener, bool removeAll = true) {
            scriptClickEvent ??= new();
            if (listener == null) return scriptClickEvent;
            if (removeAll) {
                scriptClickEvent.RemoveAllListeners();
            }
            scriptClickEvent.AddListener(listener);
            return scriptClickEvent;
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