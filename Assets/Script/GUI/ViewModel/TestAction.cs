using Cysharp.Threading.Tasks;
using Script.GameInfo.Attribute;
using Script.GUI.Screen.Interface;
using Script.LifetimeScope.Interface;
using Script.LifetimeScope.Locator;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using VContainer;

public class TestAction : MonoBehaviour {
    [AssetPath(typeof(Scene))]
    public string stageScenePath;
    
    private IScopeFactory  _scopeFactory;
    private IScreenManager _screenManager;

    [Inject]
    public void Constructor(
        IScopeFactory  scopeFactory,
        IScreenManager screenManager
    ) {
        _scopeFactory  = scopeFactory;
        _screenManager = screenManager;
    }


    public void OpenScreenTest() {
        TitleSceneLoad().Forget();
    }
    
    private async UniTask CreateGroupScope() {
        await UniTask.WaitUntil(() => (_scopeFactory != null));
        _scopeFactory.CreateScope(ScopeType.Group);
    }

    private async UniTask TitleSceneLoad() {
        await CreateGroupScope();
        await _screenManager.CloseAllAsync(true);
        var previousScene = SceneManager.GetActiveScene();

        var handle = Addressables.LoadSceneAsync(stageScenePath, LoadSceneMode.Additive);
        await handle.Task;
        var scene = handle.Result;
        SceneManager.SetActiveScene(scene.Scene);
            
        if (previousScene.IsValid() && previousScene.isLoaded) {
            var unloadOp = SceneManager.UnloadSceneAsync(previousScene);
            if (unloadOp != null)
                await unloadOp.ToUniTask();
        }
    }
}
