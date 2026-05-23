using System;
using Cysharp.Threading.Tasks;
using Script.GUI.ScreenData;
using Script.GUI.ScreenData.Interface;
using Script.Localize.Text;
using Script.Utility.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.GUI.Screen.PopUp {
    public class MessageBox : Screen {
        
        [SerializeField]
        private LocalizeText titleLocalizeText;
        
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


        private MessageBoxOption _messageBoxOption;
        
        public override UniTask OpenInternal(IScreenOption data) {
            if (data is MessageBoxOption option) {
                _messageBoxOption = option;
                
                titleText.SetText(string.IsNullOrEmpty(option.Title) ? titleLocalizeText.GetText() : _messageBoxOption.Title);
                if (string.IsNullOrEmpty(_messageBoxOption.Message) == false) {
                    messageText.SetText(_messageBoxOption.Message);
                }
                ActiveButton(_messageBoxOption);
            }

            return UniTask.CompletedTask;
        }

        private void ActiveButton(MessageBoxOption option) {
            switch (option.Type) {
                case MessageType.Ok:
                    okButton.SetActiveSafe(true);
                    okButton.ClickAddListener(option.OkAction);
                    okButton.ClickAddListener(Back, false);
                    
                    cancelButton.SetActiveSafe(false);
                    yesButton.SetActiveSafe(false);
                    noButton.SetActiveSafe(false);
                    break;
                case MessageType.OkCancel:
                    okButton.SetActiveSafe(true);
                    okButton.ClickAddListener(option.OkAction);
                    okButton.ClickAddListener(Back, false);
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
                    yesButton.ClickAddListener(Back, false);
                    noButton.SetActiveSafe(true);
                    noButton.ClickAddListener(option.NoAction);
                    noButton.ClickAddListener(Back, false);
                    break;
            }
        }

        public override UniTask CloseInternal() {
            if (_messageBoxOption != null) {
                ClassPool.Release(_messageBoxOption);
                _messageBoxOption = null;
            }
            
            return UniTask.CompletedTask;
        }
    }
}