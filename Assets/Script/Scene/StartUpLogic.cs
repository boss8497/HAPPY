using System;
using Cysharp.Threading.Tasks;
using Script.Addressable;
using Script.GameInfo.Table;
using Script.GamePlay.Scene;
using Script.GameSetting.Interface;
using Script.GUI.Screen.Interface;
using Script.LifetimeScope.Interface;
using Script.LifetimeScope.Locator;
using UnityEngine;
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

        /// <summary>
        /// LoadAppLabelsAsync()은 Local 이기 때문에 따로 Internet Check가 필요 없음
        /// </summary>
        /// <exception cref="Exception"></exception>
        private async UniTask InitializeAddressable() {
            await UniTask.WaitUntil(() => _addressable?.IsInitialized ?? false);
            await _addressable.LoadAppLabelsAsync();
        }

        private async UniTask CreateClientScope() {
            await UniTask.WaitUntil(() => (_scopeFactory != null));
            _scopeFactory.CreateScope(ScopeType.Client);
        }

        private async UniTask InitializeGameSetting() {
            _gameSetting.InitializeGameSetting();
            await UniTask.WaitUntil(() => _gameSetting?.Initialized ?? false);
        }

        private async UniTask InitializeScreenManager() {
            await UniTask.WaitUntil(() => _screenManager != null);
            _screenManager.Initialize();
            await UniTask.WaitUntil(() => _screenManager.Initialized);
        }
    }
}