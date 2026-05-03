using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Interface;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Script.GamePlay.Scene {
    public class SceneLoader : ISceneLoader {
        private readonly IScreenManager _screenManager;

        public SceneLoader(
            IScreenManager screenManager
        ) {
            _screenManager = screenManager;
        }

        public async UniTask LoadScene(string scenePath, CancellationToken ct = default) {
            await _screenManager.CloseAllAsync(true);
            await _screenManager.LoadedScreenRelease();
            var previousScene = SceneManager.GetActiveScene();
            
            var scene = await Addressables.LoadSceneAsync(scenePath, LoadSceneMode.Additive).ToUniTask(cancellationToken: ct);
            SceneManager.SetActiveScene(scene.Scene);

            if (previousScene.IsValid() && previousScene.isLoaded) {
                await SceneManager.UnloadSceneAsync(previousScene).ToUniTask(cancellationToken: ct);
            }
        }
    }
}