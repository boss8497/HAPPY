using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Enum;
using Script.GUI.ScreenData;
using Script.Utility.Runtime;
using UnityEngine.Events;

namespace Script.GUI.Screen {
    public partial class ScreenManager {
        private readonly string _errorMessagePath = nameof(ErrorMessage);

        private string _errorMessageTitle;


        public async UniTask OpenErrorMessage(ErrorMessage errorMessage, CancellationToken ct = default, object[] arguments = null) {
            if (string.IsNullOrEmpty(_errorMessageTitle)) {
                _errorMessageTitle = await _localize.GetErrorMessage(ErrorMessage.Error);
            }

            var message = await _localize.GetErrorMessage(errorMessage);

            var option = ClassPool.Get<MessageBoxOption>();
            option.Set(
                _errorMessageTitle,
                message,
                arguments
            );

            await OpenAsync(option, _errorMessagePath, ct);
        }

        public async UniTask OpenMessage(
            string            title,
            string            message,
            object[]          arguments    = null,
            MessageType       messageType  = MessageType.Ok,
            UnityAction       okAction     = null,
            UnityAction       cancelAction = null,
            UnityAction       yesAction    = null,
            UnityAction       noAction     = null,
            CancellationToken ct           = default
        ) {
            if (string.IsNullOrEmpty(_errorMessageTitle)) {
                _errorMessageTitle = await _localize.GetErrorMessage(ErrorMessage.Error);
            }

            var option = ClassPool.Get<MessageBoxOption>();
            option.Set(
                title,
                message,
                arguments,
                messageType,
                okAction,
                cancelAction,
                yesAction,
                noAction
            );

            await OpenAsync(option, _errorMessagePath, ct);
        }
    }
}