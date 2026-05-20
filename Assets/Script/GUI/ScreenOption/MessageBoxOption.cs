using System;
using Script.GUI.ScreenData.Interface;
using UnityEngine.Events;

namespace Script.GUI.ScreenData {
    public enum MessageType {
        Ok,
        OkCancel,
        YesNo,
    }

    public class MessageBoxOption : IScreenOption {
        public string      Title   { get; set; }
        public string      Message { get; set; }
        public MessageType Type    { get; set; }

        public UnityAction OkAction     { get; set; }
        public UnityAction CancelAction { get; set; }
        public UnityAction YesAction    { get; set; }
        public UnityAction NoAction     { get; set; }

        public MessageBoxOption(
            string      title,
            string      message,
            UnityAction okAction,
            UnityAction cancelAction,
            UnityAction yesAction,
            UnityAction noAction,
            MessageType messageType = MessageType.Ok
        ) {
            Title        = title;
            Message      = message;
            OkAction     = okAction;
            CancelAction = cancelAction;
            YesAction    = yesAction;
            NoAction     = noAction;
            Type         = messageType;
        }
    }
}