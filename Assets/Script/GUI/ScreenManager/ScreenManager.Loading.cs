using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;
using UnityEngine;

namespace Script.GUI.Screen {
    /// <summary>
    /// StageTransition 위에 뜨는 로딩 화면.
    /// 자주 열고 닫혀 매번 Addressable 로드/Destroy 하기엔 낭비라, 한 번 로드한 뒤
    /// _loadedScreens(ResourceClear 대상)에는 넣지 않고 이 파일의 필드로만 계속 들고 있는다.
    /// </summary>
    public partial class ScreenManager {
        private readonly string _loadingScreenKey = "Loading";

        // ※ ScreenManagerDebugWindow.cs가 리플렉션으로 참조함 — 필드명은 아래 FieldName 상수(nameof)로만 참조할 것
        public const string LoadingScreenFieldName      = nameof(_loadingScreen);
        public const string LoadingScreenShownFieldName = nameof(_loadingScreenShown);

        private IScreen _loadingScreen;
        private bool    _loadingScreenShown;

        private async UniTask ShowLoadingAsync() {
            if (_loadingScreenShown) return;

            if (_loadingScreen == null) {
                if (_screens.TryGetValue(_loadingScreenKey, out var screenAsset) == false) {
                    Debug.LogError($"Screen ID {_loadingScreenKey} not found");
                    return;
                }

                var obj = await LoadScreen(screenAsset.screen);
                _loadingScreen = obj.GetComponent<IScreen>();
            }

            _loadingScreenShown = true;
            await _layers[(int)ScreenLayerType.Loading].OpenScreen(_loadingScreen, null);
        }

        private async UniTask HideLoadingAsync() {
            if (_loadingScreenShown == false) return;

            _loadingScreenShown = false;
            await _layers[(int)ScreenLayerType.Loading].CloseScreen(_loadingScreen);
        }
    }
}
