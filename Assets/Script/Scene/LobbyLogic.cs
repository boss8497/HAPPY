using Cysharp.Threading.Tasks;
using Script.GameInfo.Attribute;
using Script.GamePlay.Audio.Interface;
using Script.GUI.Screen.Interface;
using UnityEngine;
using VContainer;

namespace Script.Scene {
    public class LobbyLogic : MonoBehaviour {
        private IScreenManager _screenManager;
        private IAudioManager  _audioManager;

        [Inject]
        public void Constructor(
            IScreenManager screenManager,
            IAudioManager audioManager
        ) {
            _screenManager = screenManager;
            _audioManager = audioManager;
        }

        [ScreenKey]
        public string hudKey;

        private void Start() {
            Initialize().Forget();
        }

        private async UniTask Initialize() {
            _audioManager.StopBGM();
            await UniTask.WaitUntil(() => _screenManager?.Initialized ?? false);
            await _screenManager.OpenAsync(hudKey);
        }
    }
}