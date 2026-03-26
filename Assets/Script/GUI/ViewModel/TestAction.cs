using Cysharp.Threading.Tasks;
using Script.GameInfo.Attribute;
using Script.GUI.Interface;
using Script.LifetimeScope.Locator;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using VContainer;

public class TestAction : MonoBehaviour {
    [AssetPath(typeof(Scene))]
    public string stageScenePath;
    
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


    public void OpenScreenTest() {
        _screenManager.OpenScreen("Test");
        TitleSceneLoad().Forget();
    }
    
    // private async UniTask CreateGroupScope() {
    //     await UniTask.WaitUntil(() => (_scopeLocator?.Initialized ?? false));
    //
    //     var clientScope = _scopeLocator.GetRootScope().CreateChild<GroupLifetimeScope>();
    //     _scopeLocator.SetScope(ScopeType.Client, clientScope);
    // }

    private async UniTask TitleSceneLoad() {
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
