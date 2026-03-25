using System;
using Cysharp.Threading.Tasks;
using Script.GUI.Interface;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using VContainer.Unity;


namespace Script.StartUp {
    public class GameStartUp : IStartable {
        private readonly string startUpSceneName = "StartUp";
        private readonly string titleScenePath   = "Title";

        private readonly IScreenManager _screenManager;

        public GameStartUp(
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