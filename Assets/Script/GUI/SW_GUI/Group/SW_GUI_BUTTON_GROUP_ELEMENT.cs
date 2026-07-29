using SW.GUI.Base;
using UnityEngine;
using UnityEngine.Events;

namespace SW.GUI {
    public class SW_GUI_BUTTON_GROUP_ELEMENT : SW_GUI_BUTTON_GROUP_ELEMENT_BASE {
        public override SW_GUI_BUTTON_GROUP_BASE Group { get; set; }

        
        public UnityEvent OnSelect;
        public UnityEvent OnDeselect;

        public override void Initialize() { }

        protected override void OnSelectEvent() {
            OnSelect?.Invoke();
        }

        protected override void OnDeselectEvent() {
            OnDeselect?.Invoke();
        }

        
#if UNITY_EDITOR
        /// <summary>
        /// 인스펙터에서 컴포넌트를 처음 추가할 때 Parent 계층에서 그룹을 찾아 자동 등록.
        /// 못 찾으면 Parent에 SW_GUI_BUTTON_GROUP을 새로 추가한다.
        /// </summary>
        private void Reset() {
            if (transform.parent == null) {
                Debug.LogWarning($"[{name}] SW_GUI_BUTTON_GROUP_ELEMENT는 Parent가 필요합니다. 그룹 자동 등록을 건너뜁니다.", this);
                return;
            }

            var group = transform.parent.GetComponentInParent<SW_GUI_BUTTON_GROUP_BASE>();
            if (group == null) {
                group = transform.parent.gameObject.AddComponent<SW_GUI_BUTTON_GROUP>();
                Debug.Log($"[{name}] Parent '{transform.parent.name}'에 SW_GUI_BUTTON_GROUP_BASE가 없어 SW_GUI_BUTTON_GROUP을 자동으로 추가했습니다.", transform.parent);
            }

            if (Group != null && Group != group) {
                Group.Unregister(this);
            }

            group.Register(this);
        }
#endif
    }
}