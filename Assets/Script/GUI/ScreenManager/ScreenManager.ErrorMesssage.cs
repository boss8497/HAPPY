using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Enum;
using Script.GUI.ScreenData;
using Script.Utility.Runtime;

namespace Script.GUI.Screen {
    public partial class ScreenManager {
        private readonly string _errorMessagePath = nameof(ErrorMessage);

        private string _errorMessageTitle;


        public async UniTask OpenErrorMessage(ErrorMessage errorMessage, CancellationToken ct) {
            if (string.IsNullOrEmpty(_errorMessageTitle)) {
                _errorMessageTitle = await _localize.GetErrorMessage(ErrorMessage.Error);
            }

            var message = await _localize.GetErrorMessage(errorMessage);

            var option = ClassPool.Get<MessageBoxOption>();
            option.Set(
                _errorMessageTitle,
                message
            );

            await OpenAsync(option, _errorMessagePath, ct);
        }
    }
}