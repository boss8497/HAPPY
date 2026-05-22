using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Script.Addressable;
using Script.GameInfo.Table;
using Script.GamePlay.Scene;
using Script.GameSetting.Interface;
using Script.GUI.Screen.Interface;
using Script.LifetimeScope.Interface;
using Script.LifetimeScope.Locator;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using VContainer;


namespace Script.Scene {
    public class StartUpLogic : MonoBehaviour {
        private IAddressable   _addressable;
        private IScopeFactory  _scopeFactory;
        private IGameSetting   _gameSetting;
        private IScreenManager _screenManager;
        private ISceneLoader   _sceneLoader;

        [Inject]
        public void Constructor(
            IAddressable   addressable,
            IScopeFactory  scopeFactory,
            IGameSetting   gameSetting,
            IScreenManager screenManager,
            ISceneLoader   sceneLoader
        ) {
            _addressable   = addressable;
            _scopeFactory  = scopeFactory;
            _gameSetting   = gameSetting;
            _screenManager = screenManager;
            _sceneLoader   = sceneLoader;
        }

        public void Start() {
            Initialize().Forget();
        }


        private async UniTaskVoid Initialize() {
            await InitializeAddressable();
            await InitializeGameSetting();
            await InitializeScreenManager();
            await CreateClientScope();

            await _sceneLoader.LoadScene(GameInfoManager.Instance.Config.titleScenePath);
        }

        private async UniTask InitializeAddressable() {
            await UniTask.WaitUntil(() => _addressable?.IsInitialized ?? false);
            var result = await _addressable.HasInternetConnectionAsync();
            if (result == false) {
                throw new Exception("No internet connection.");
            }
            await _addressable.LoadAppLabelsAsync();
        }

        private async UniTask CreateClientScope() {
            await UniTask.WaitUntil(() => (_scopeFactory != null));
            _scopeFactory.CreateScope(ScopeType.Client);
        }

        private async UniTask InitializeGameSetting() {
            await UniTask.WaitUntil(() => _gameSetting?.Initialized ?? false);
        }

        private async UniTask InitializeScreenManager() {
            await UniTask.WaitUntil(() => _screenManager != null);
            _screenManager.Initialize();
            await UniTask.WaitUntil(() => _screenManager.Initialized);
        }
    }
}