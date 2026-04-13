using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Script.GUI.Screen {
    public abstract partial class Screen {
        [LabelText("뒤로가기 버튼 자동 등록")]
        [SerializeField]
        private Button[] _backButtons = Array.Empty<Button>();
        
        private void Awake() {
            SetBackButtons();
            AwakeInternal();
        }

        protected virtual void AwakeInternal() {
        }

        private void SetBackButtons() {
            foreach (var backButton in _backButtons) {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(Back);
            }
        }
    }
}