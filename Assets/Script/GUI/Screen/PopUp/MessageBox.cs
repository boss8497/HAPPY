using System;
using Cysharp.Threading.Tasks;
using Script.GUI.ScreenData;
using Script.GUI.ScreenData.Interface;
using Script.Utility.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.GUI.Screen.PopUp {
    public class MessageBox : Screen {
        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text messageText;


        [SerializeField]
        private Button okButton;

        [SerializeField]
        private Button cancelButton;

        [SerializeField]
        private Button yesButton;

        [SerializeField]
        private Button noButton;


        public override UniTask OpenInternal(IScreenOption data) {
            if (data is MessageBoxOption option) {
                if (string.IsNullOrEmpty(option.Title) == false) {
                    titleText.SetText(option.Title);
                }
                if (string.IsNullOrEmpty(option.Message) == false) {
                    messageText.SetText(option.Message);
                }
                ActiveButton(option);
            }

            return UniTask.CompletedTask;
        }

        private void ActiveButton(MessageBoxOption option) {
            switch (option.Type) {
                case MessageType.Ok:
                    okButton.SetActiveSafe(true);
                    okButton.ClickAddListener(option.OkAction);
                    
                    cancelButton.SetActiveSafe(false);
                    yesButton.SetActiveSafe(false);
                    noButton.SetActiveSafe(false);
                    break;
                case MessageType.OkCancel:
                    okButton.SetActiveSafe(true);
                    okButton.ClickAddListener(option.OkAction);
                    cancelButton.SetActiveSafe(true);
                    cancelButton.ClickAddListener(option.CancelAction);
                    cancelButton.ClickAddListener(Back, false);
                    
                    yesButton.SetActiveSafe(false);
                    noButton.SetActiveSafe(false);
                    break;
                case MessageType.YesNo:
                    okButton.SetActiveSafe(false);
                    cancelButton.SetActiveSafe(false);
                    
                    yesButton.SetActiveSafe(true);
                    yesButton.ClickAddListener(option.YesAction);
                    noButton.SetActiveSafe(true);
                    noButton.ClickAddListener(option.NoAction);
                    noButton.ClickAddListener(Back, false);
                    break;
            }
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}