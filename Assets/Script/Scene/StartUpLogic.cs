using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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
        private IScopeFactory  _scopeFactory;
        private IGameSetting   _gameSetting;
        private IScreenManager _screenManager;
        private ISceneLoader   _sceneLoader;

        [Inject]
        public void Constructor(
            IScopeFactory  scopeFactory,
            IGameSetting   gameSetting,
            IScreenManager screenManager,
            ISceneLoader   sceneLoader
        ) {
            _scopeFactory  = scopeFactory;
            _gameSetting   = gameSetting;
            _screenManager = screenManager;
            _sceneLoader   = sceneLoader;
        }

        public void Start() {
            Initialize().Forget();
        }


        private async UniTaskVoid Initialize() {
            await InitializeGameSetting();
            await InitializeScreenManager();
            await CreateClientScope();
            await _sceneLoader.LoadScene(_gameSetting.TitleScenePath);
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