using Script.GameInfo.Enum;
using Script.GUI.ScreenData.Interface;

namespace Script.GUI.ScreenData {
    public class MessageBoxError : IScreenOption {
        public MessageType  Type  { get; set; }
        public ErrorMessage Error { get; set; }
        

        public MessageBoxError(
            ErrorMessage error,
            MessageType  messageType = MessageType.Ok
        ) {
            Type  = messageType;
            Error = error;
        }
    }
}