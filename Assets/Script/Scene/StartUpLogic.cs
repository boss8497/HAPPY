using System;
using Cysharp.Threading.Tasks;
using Script.Addressable;
using Script.GameInfo.Table;
using Script.GamePlay.Audio.Interface;
using Script.GamePlay.Scene;
using Script.GameSetting.Interface;
using Script.GUI.Screen.Interface;
using Script.LifetimeScope.Interface;
using Script.LifetimeScope.Locator;
using UnityEngine;
using VContainer;


namespace Script.Scene {
    public class StartUpLogic : MonoBehaviour {
        private IAddressableService   _addressableService;
        private IScopeFactory  _scopeFactory;
        private IGameSetting   _gameSetting;
        private IAudioManager  _audioManager;
        private IScreenManager _screenManager;
        private ISceneLoader   _sceneLoader;

        [Inject]
        public void Constructor(
            IAddressableService   addressableService,
            IScopeFactory  scopeFactory,
            IGameSetting   gameSetting,
            IAudioManager  audioManager,
            IScreenManager screenManager,
            ISceneLoader   sceneLoader
        ) {
            _addressableService   = addressableService;
            _scopeFactory  = scopeFactory;
            _gameSetting   = gameSetting;
            _audioManager  = audioManager;
            _screenManager = screenManager;
            _sceneLoader   = sceneLoader;
        }

        public void Start() {
            Initialize().Forget();
        }


        private async UniTaskVoid Initialize() {
            await InitializeAddressable();
            await InitializeGameSetting();
            await InitializeAudioManager();
            await InitializeScreenManager();
            await CreateClientScope();

            await _sceneLoader.LoadScene(GameInfoManager.Instance.Config.titleScenePath);
        }

        /// <summary>
        /// LoadAppLabelsAsync()은 Local 이기 때문에 따로 Internet Check가 필요 없음
        /// </summary>
        /// <exception cref="Exception"></exception>
        private async UniTask InitializeAddressable() {
            await UniTask.WaitUntil(() => _addressableService?.IsInitialized ?? false);
            await _addressableService.LoadAppLabelsAsync();
        }

        private async UniTask CreateClientScope() {
            await UniTask.WaitUntil(() => (_scopeFactory != null));
            _scopeFactory.CreateScope(ScopeType.Client);
        }

        private async UniTask InitializeGameSetting() {
            _gameSetting.InitializeGameSetting();
            await UniTask.WaitUntil(() => _gameSetting?.Initialized ?? false);
        }

        private async UniTask InitializeAudioManager() {
            _audioManager.InitializeAudioManager();
            await UniTask.WaitUntil(() => _audioManager?.Initialized ?? false);
        }

        private async UniTask InitializeScreenManager() {
            await UniTask.WaitUntil(() => _screenManager != null);
            _screenManager.Initialize();
            await UniTask.WaitUntil(() => _screenManager.Initialized);
        }
    }
}