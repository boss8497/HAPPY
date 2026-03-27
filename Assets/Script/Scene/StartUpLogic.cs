using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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

        [Inject]
        public void Constructor(
            IScopeFactory  scopeFactory,
            IGameSetting   gameSetting,
            IScreenManager screenManager
        ) {
            _scopeFactory  = scopeFactory;
            _gameSetting   = gameSetting;
            _screenManager = screenManager;
        }

        public void Start() {
            Initialize().Forget();
        }


        private async UniTaskVoid Initialize() {
            await InitializeGameSetting();
            await InitializeScreenManager();
            await CreateClientScope();
            await TitleSceneLoad();
        }

        private async UniTask CreateClientScope() {
            await UniTask.WaitUntil(() => (_scopeFactory != null));
            _scopeFactory.CreateScope(ScopeType.Client);
        }

        private async UniTask TitleSceneLoad() {
            var previousScene = SceneManager.GetActiveScene();

            var handle = Addressables.LoadSceneAsync(_gameSetting.TitleScenePath, LoadSceneMode.Additive);
            await handle.Task;
            var scene = handle.Result;
            SceneManager.SetActiveScene(scene.Scene);

            if (previousScene.IsValid() && previousScene.isLoaded) {
                var unloadOp = SceneManager.UnloadSceneAsync(previousScene);
                if (unloadOp != null)
                    await unloadOp.ToUniTask();
            }
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