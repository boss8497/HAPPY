using SW.GUI.Base;
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
    }
}