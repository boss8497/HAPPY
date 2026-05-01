using Cysharp.Threading.Tasks;
using Script.GameInfo.Dungeon;
using Script.GamePlay.Service.Interface;
using Script.GUI.Screen.Interface;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Script.GamePlay.Stage {
    public class StageLoader : IStageLoader {
        private readonly IGroupService _groupService;
        private readonly IScreenManager _screenManager;

        public StageLoader(
            IGroupService  groupService,
            IScreenManager screenManager
        ) {
            _groupService = groupService;
            _screenManager = screenManager;
        }

        public async UniTask LoadStage(GameInfo.Dungeon.Stage stage) {
            await _screenManager.CloseAllAsync(true);
            var previousScene = SceneManager.GetActiveScene();

            
            var handle = Addressables.LoadSceneAsync(stage.scenePath, LoadSceneMode.Additive);
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
}