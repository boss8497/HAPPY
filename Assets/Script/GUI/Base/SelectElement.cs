using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Script.GUI {
    public abstract class SelectElement : MonoBehaviour {
        [LabelText("선택된 상태에서 다시 선택 시 선택 해제 여부")]
        public bool selectedToDeSelect;

        public UnityEvent OnSelect;
        public UnityEvent OnDeSelect;


        private Selector _selector;

        public Selector Selector {
            get => _selector;
            set {
                if (_selector != null) {
                    _selector.Unregister(this);
                }

                _selector = value;
                if (_selector != null) {
                    _selector.Register(this);
                }
            }
        }


        private bool _selected;

        /// <summary>
        /// 조건
        /// Selected는 Element에서 직접 true, false를 해주지 말고 Selector에서 설정하자!!
        /// </summary>
        public bool Selected {
            get => _selected;
            set {
                _selected = value;
                if (_selected) {
                    OnSelectInternal();
                    OnSelect?.Invoke();
                }
                else {
                    OnDeSelectInternal();
                    OnDeSelect?.Invoke();
                }
            }
        }

        protected int _key;
        public    int Key => _key;

        // public int Key {
        //     get => _key;
        //     set {
        //         if (_selector != null) {
        //             _selector.Unregister(this);
        //         }
        //         
        //         _key = value;
        //         _selector.Register(this);
        //     }
        // }

        #region UnityEvents

        private void Awake() {
            SetKey();
        }

        /// <summary>
        /// _key 필드 값 셋팅
        /// key별로 group 나눠짐
        /// </summary>
        protected abstract void SetKey();

        public void OnEnable() {
            _selector = GetComponentInParent<Selector>();
            if (_selector == null) return;
            _selector.Register(this);
            OnEnableInternal();
        }

        protected virtual void OnEnableInternal() { }

        private void OnDestroy() {
            if (_selector != null) {
                _selector.Unregister(this);
            }

            OnDestroyInternal();
        }

        protected virtual void OnDestroyInternal() { }

        #endregion

        public void Select() {
            if (_selector == null) return;
            
            if (Selected) {
                if (selectedToDeSelect) {
                    Deselect();
                }

                return;
            }

            _selector.Select(this);
        }

        public void Deselect() {
            if (_selector == null) return;
            
            _selector.DeSelect(this);
        }

        public abstract void OnSelectInternal();
        public abstract void OnDeSelectInternal();
    }
}