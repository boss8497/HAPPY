using System.Linq;
using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Script.GamePlay.Scene;
using Script.Utility.Runtime;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class RunningStageScreen : Screen {
        // Reactive
        private IGroupService   _groupService;
        private IObjectResolver _objectResolver;
        private ISceneLoader    _sceneLoader;

        [Inject]
        public void InjectSelf(
            IGroupService   groupService,
            IObjectResolver objectResolver,
            ISceneLoader    sceneLoader
        ) {
            _groupService   = groupService;
            _objectResolver = objectResolver;
            _sceneLoader    = sceneLoader;
        }


        // Private Field
        private DungeonProgress _dungeonProgress;
        private DungeonInfo     _dungeonInfo;
        private Stage           _stage;

        // Inspector
        public Button testStartBtn;

        protected override void AwakeInternal() {
            base.AwakeInternal();

            if (testStartBtn != null) {
                testStartBtn.ClickAddListener(() => {
                    _sceneLoader.LoadScene(_stage.scenePath).Forget();
                });
            }
        }

        public override UniTask OpenInternal() {
            _dungeonProgress = _groupService.GetDungeon(Category.Running);
            _dungeonInfo     = GameInfoManager.Instance.Get<DungeonInfo>(_dungeonProgress.dungeonUid);
            _stage           = _dungeonInfo.stages.FirstOrDefault(r => r.guid.Value == _dungeonProgress.stageGuid);
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}