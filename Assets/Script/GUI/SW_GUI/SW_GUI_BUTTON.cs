using Sirenix.OdinInspector;
using SW.GUI.Base;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SW.GUI {
    /// <summary>
    /// Simple 보다는 기능이 조금 더 추가 되어있는 형태
    /// </summary>
    public class SW_GUI_BUTTON : SW_GUI_BUTTON_BASE {
        #region Inspector

        [SerializeField]
        [OnValueChanged("InteractableGraphicChanged")]
        private Graphic _targetGraphic;

        [ToggleGroup("_interactable"), ShowIf("_interactable")]
        [SerializeField]
        private Color _interactableColor = new Color(210f, 210f, 210f, 0.5f);

        [SerializeField]
        private Color _baseGraphicColor;

        #endregion

        public override void Initialize() {
            _baseGraphicColor = _targetGraphic?.color ?? Color.white;
        }

        public override void OnClick() { }


        private Button test;

        private void InteractableGraphicColor() {
            if (_targetGraphic == null) {
                return;
            }
            _targetGraphic.color = _interactable ? _baseGraphicColor : _baseGraphicColor * _interactableColor;
        }

        #region Odin

        protected override void InteractableChanged() {
            InteractableGraphicColor();
        }

        private void InteractableGraphicChanged() {
            void ChangedColor() {
                if (_interactable)
                    _baseGraphicColor = _targetGraphic?.color ?? Color.white;
            }

            if (_targetGraphic != null) {
                _targetGraphic.UnregisterDirtyVerticesCallback(ChangedColor);
                _targetGraphic.RegisterDirtyVerticesCallback(ChangedColor);
            }

            ChangedColor();
            InteractableGraphicColor();
        }

        #endregion


        #region Editor

#if UNITY_EDITOR
        private void Reset() {
            _targetGraphic    = GetComponent<Graphic>();
            _baseGraphicColor = _targetGraphic?.color ?? Color.white;
            InteractableGraphicChanged();
        }
#endif

        #endregion
    }
}