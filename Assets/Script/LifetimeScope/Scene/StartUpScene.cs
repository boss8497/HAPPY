using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Script.GUI.Interface;
using Script.LifetimeScope.Locator;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using VContainer;


namespace Script.LifetimeScope.Scene {
    public class StartUpScene : MonoBehaviour {
        private readonly string titleScenePath = "Title";

        private IScopeLocator  _scopeLocator;
        private IScreenManager _screenManager;

        [Inject]
        public void Constructor(
            IScopeLocator  scopeLocator,
            IScreenManager screenManager
        ) {
            _scopeLocator  = scopeLocator;
            _screenManager = screenManager;
        }

        public void Start() {
            Initialize().Forget();
        }


        private async UniTaskVoid Initialize() {
            await InitializeScreenManager();
            await CreateClientScope();
            await TitleSceneLoad();
        }

        private async UniTask CreateClientScope() {
            await UniTask.WaitUntil(() => (_scopeLocator?.Initialized ?? false));

            var clientScope = _scopeLocator.GetRootScope().CreateChild<ClientLifetimeScope>();
            _scopeLocator.SetScope(ScopeType.Client, clientScope);
        }

        private async UniTask TitleSceneLoad() {
            var previousScene = SceneManager.GetActiveScene();

            var handle = Addressables.LoadSceneAsync(titleScenePath, LoadSceneMode.Additive);
            await handle.Task;
            var scene = handle.Result;
            SceneManager.SetActiveScene(scene.Scene);
            
            if (previousScene.IsValid() && previousScene.isLoaded) {
                var unloadOp = SceneManager.UnloadSceneAsync(previousScene);
                if (unloadOp != null)
                    await unloadOp.ToUniTask();
            }
        }

        private async UniTask InitializeScreenManager() {
            await UniTask.WaitUntil(() => _screenManager != null);
            _screenManager.Initialize();
            await UniTask.WaitUntil(() => _screenManager.Initialized);
        }
    }
}