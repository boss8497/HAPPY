

namespace SW.GUI.Base {
    public enum ElementOption {
        None,
        SelectToDeSelect,
    }

    public abstract class SW_GUI_BUTTON_GROUP_ELEMENT_BASE : SW_GUI_BUTTON_BASE {
        public abstract SW_GUI_BUTTON_GROUP_BASE Group { get; set; }

        public int Key   { get; set; } = -1;
        public int Order { get; set; } = 0;

        /// <summary>
        /// 선택 여부 변경은 Group에서 해준다.
        /// 절대 상속받은 곧에서 건들이지 않는다는 조건
        /// </summary>
        private bool _selected = false;

        public bool Selected {
            get => _selected;
            set {
                _selected = value;
                if (_selected) {
                    OnSelectEvent();
                }
                else {
                    OnDeselectEvent();
                }
            }
        }

        #region Inspector

        public ElementOption option = ElementOption.None;

        #endregion


        public abstract override void Initialize();

        public override void OnClick() {
            if (Group == null) {
                return;
            }

            Group.Select(this);
        }

        protected abstract void OnSelectEvent();
        protected abstract void OnDeselectEvent();
    }
}