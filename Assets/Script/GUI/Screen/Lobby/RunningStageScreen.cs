using System.Linq;
using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Script.GamePlay.Scene;
using Script.GUI.ScreenData.Interface;
using Script.GUI.ViewModel;
using Script.Utility.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class RunningStageScreen : Screen {
        // Reactive
        private IGroupService   _groupService;
        private IObjectResolver _objectResolver;

        [Inject]
        public void InjectSelf(
            IGroupService   groupService,
            IObjectResolver objectResolver
        ) {
            _groupService   = groupService;
            _objectResolver = objectResolver;
        }


        // Private Field
        private DungeonProgress _dungeonProgress;
        private DungeonInfo     _dungeonInfo;
        private Stage           _stage;

        
        // Inspector
        public AssetReferenceT<GameObject> element;
        public RectTransform               contentRoot;
        
        public override async UniTask OpenInternal(IScreenOption data) {
            await UniTask.WaitUntil(() => _groupService.Initialized);
            _dungeonProgress = _groupService.GetDungeon(Category.Running);
            _dungeonInfo     = GameInfoManager.Instance.Get<DungeonInfo>(_dungeonProgress.dungeonUid);
            _stage           = _dungeonInfo.stages.FirstOrDefault(r => r.guid.Value == _dungeonProgress.stageGuid);

            foreach (var stage in _dungeonInfo.stages) {
                var obj          = PoolPop(element.AssetGUID, contentRoot);
                var stageElement = obj.GetComponent<StageElement>();
                if (stageElement != null) {
                    stageElement.InitializeReactive();
                    stageElement.SetReactive(stage, _dungeonInfo);
                    
                    if(stageElement.startBtn != null) {
                        stageElement.startBtn.ClickAddListener(() => {
                            if (stageElement.Stage?.CurrentValue == null || stageElement.DungeonInfo?.CurrentValue == null) return;
                            _groupService.EnterDungeon(stageElement.DungeonInfo.CurrentValue, stageElement.Stage.CurrentValue).Forget();
                        });
                    }
                }
            }
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}