using Cysharp.Threading.Tasks;
using Script.GameInfo.Attribute;
using Script.GUI.Screen.Interface;
using UnityEngine;
using VContainer;

namespace Script.Scene {
    public class LobbyLogic : MonoBehaviour {
        private IScreenManager _screenManager;

        [Inject]
        public void Constructor(
            IScreenManager screenManager
        ) {
            _screenManager = screenManager;
        }

        [ScreenKey]
        public string hudKey;

        private void Start() {
            Initialize().Forget();
        }

        private async UniTask Initialize() {
            await UniTask.WaitUntil(() => _screenManager?.Initialized ?? false);
            await _screenManager.OpenAsync(hudKey);
        }
    }
}