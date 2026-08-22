using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;
using UnityEngine;

namespace Script.GUI.Screen {
    public partial class ScreenManager {
        private readonly string _safeAreaScreenKey = "SafeArea";

        private IScreen _safeAreaScreen;
        private bool    _safeAreaScreenShown;

        public async UniTask ShowSafeAreaAsync() {
            if (_safeAreaScreenShown) return;

            if (_safeAreaScreen == null) {
                if (_screens.TryGetValue(_safeAreaScreenKey, out var screenAsset) == false) {
                    Debug.LogError($"Screen ID {_safeAreaScreenKey} not found");
                    return;
                }

                var obj = await LoadScreen(screenAsset.screen);
                _safeAreaScreen = obj.GetComponent<IScreen>();
            }

            _safeAreaScreenShown = true;
            await _layers[(int)ScreenLayerType.SafeArea].OpenScreen(_safeAreaScreen, null);
        }

        public async UniTask HideSafeAreaAsync() {
            if (_safeAreaScreenShown == false) return;

            _safeAreaScreenShown = false;
            await _layers[(int)ScreenLayerType.SafeArea].CloseScreen(_safeAreaScreen);
        }
    }
}