using System;
using Script.GUI.ScreenData.Interface;
using Script.Utility.Runtime;
using UnityEngine.Events;

namespace Script.GUI.ScreenData {
    public enum MessageType {
        Ok,
        OkCancel,
        YesNo,
    }

    public class MessageBoxOption : IScreenOption, IClassPool {
        public string      Title   { get; set; }
        public string      Message { get; set; }
        public MessageType Type    { get; set; }

        public UnityAction OkAction     { get; set; }
        public UnityAction CancelAction { get; set; }
        public UnityAction YesAction    { get; set; }
        public UnityAction NoAction     { get; set; }

        public MessageBoxOption() {
            Title        = string.Empty;
            Message      = string.Empty;
            OkAction     = null;
            CancelAction = null;
            YesAction    = null;
            NoAction     = null;
        }

        public MessageBoxOption(
            string      title,
            string      message,
            MessageType messageType  = MessageType.Ok,
            UnityAction okAction     = null,
            UnityAction cancelAction = null,
            UnityAction yesAction    = null,
            UnityAction noAction     = null
        ) {
            Title        = title;
            Message      = message;
            OkAction     = okAction;
            CancelAction = cancelAction;
            YesAction    = yesAction;
            NoAction     = noAction;
            Type         = messageType;
        }

        public void Set(
            string      title,
            string      message,
            MessageType messageType  = MessageType.Ok,
            UnityAction okAction     = null,
            UnityAction cancelAction = null,
            UnityAction yesAction    = null,
            UnityAction noAction     = null
        ) {
            Title        = title;
            Message      = message;
            OkAction     = okAction;
            CancelAction = cancelAction;
            YesAction    = yesAction;
            NoAction     = noAction;
            Type         = messageType;
        }

        public void OnRent() {
        }
        public void OnReturn() {
            Title        = string.Empty;
            Message      = string.Empty;
            OkAction     = null;
            CancelAction = null;
            YesAction    = null;
            NoAction     = null;
        }
    }
}