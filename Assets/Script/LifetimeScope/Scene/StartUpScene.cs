using System;
using Cysharp.Threading.Tasks;
using Script.GUI.Interface;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using VContainer;


namespace Script.LifetimeScope.Scene {
    public class StartUpScene : MonoBehaviour {
        private readonly string titleScenePath = "Title";

        private IScreenManager _screenManager;

        [Inject]
        public void Constructor(
            IScreenManager screenManager
        ) {
            _screenManager = screenManager;
        }

        public void Start() {
            Initialize().Forget();
        }


        private async UniTaskVoid Initialize() {
            await InitializeScreenManager();
            await TitleSceneLoad();
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