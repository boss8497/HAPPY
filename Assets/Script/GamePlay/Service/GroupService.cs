using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.Client;
using Script.DataBase.Enum;
using Script.DataBase.Interface;
using Script.GameData.Data;
using Script.GameData.Data.Interface;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Script.GamePlay.Scene;
using Script.GamePlay.Service.Interface;
using Script.Utility.Runtime;
using UnityEngine;
using VContainer.Unity;

namespace Script.GamePlay.Service {
    public class GroupService : IGroupService, IInitializable {
        private readonly IClient      _client;
        private readonly IItemService _itemService;
        private readonly ISceneLoader _sceneLoader;

        private GroupData  _groupData;
        public  IGroupData GroupData => _groupData;
        public  long       GroupUid  => _groupData?.Model?.CurrentValue?.uid ?? 0;

        public bool Initialized { get; private set; }


        private Tuple<DungeonInfo, Stage> _enterDungeon;

        public GroupService(
            IClient      client,
            IItemService itemService,
            ISceneLoader sceneLoader
        ) {
            _client      = client;
            _itemService = itemService;
            _sceneLoader = sceneLoader;
        }

        public void Initialize() {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync() {
            //첫 접속
            var model = await _client.Req_Group();
            _groupData  = new GroupData(model);
            
            var items = await _client.Req_Inventory(GroupUid);
            await _itemService.InitializeAsync(items);
            
            Initialized = true;
        }

        public async UniTask EnterDungeon(DungeonInfo dungeonInfo, Stage stage) {
            var result = await _client.Req_EnterDungeon(dungeonInfo, stage);
            if (result) {
                _enterDungeon = new(dungeonInfo, stage);
                await _sceneLoader.LoadScene(stage.scenePath);
            }
        }

        public Tuple<DungeonInfo, Stage> GetEnterDungeon() {
            return _enterDungeon;
        }

        public DungeonProgress GetDungeon(Category dungeonCategory) {
            var category = (int)dungeonCategory;
            return _groupData.Model.CurrentValue.dungeonProgresses?.FirstOrDefault(r => r.category == category);
        }

        public bool IsCleared(DungeonInfo dungeonInfo, Stage stage) {
            var dungeonProgress = GetDungeon((Category)dungeonInfo.category);
            if (dungeonProgress == null) {
                Debug.LogError($"Not found Category: {(Category)dungeonInfo.category}");
                return false;
            }

            if (dungeonProgress.dungeonUid != dungeonInfo.UID) {
                Debug.LogError($"Not found Dungeon {dungeonInfo.UID}");
                return false;
            }

            var stageIndex   = dungeonInfo.stages.FindIndex(r => r.guid.Value == stage.guid.Value);
            var clearedIndex = dungeonInfo.stages.FindIndex(r => r.guid.Value == dungeonProgress.stageGuid);

            return dungeonProgress.cleared ? stageIndex <= clearedIndex : stageIndex < clearedIndex;
        }

        public bool CanEnterStage(DungeonInfo dungeonInfo, Stage stage) {
            var dungeonProgress = GetDungeon((Category)dungeonInfo.category);
            if (dungeonProgress == null) {
                Debug.LogError($"Not found Category: {(Category)dungeonInfo.category}");
                return false;
            }

            if (dungeonProgress.dungeonUid != dungeonInfo.UID) {
                Debug.LogError($"Not found Dungeon {dungeonInfo.UID}");
                return false;
            }

            var stageIndex   = dungeonInfo.stages.FindIndex(r => r.guid.Value == stage.guid.Value);
            var clearedIndex = dungeonInfo.stages.FindIndex(r => r.guid.Value == dungeonProgress.stageGuid);

            return stageIndex <= clearedIndex;
        }

        public async UniTask ClearedDungeon(DungeonInfo dungeonInfo, Stage stage) {
            var items = await _client.Req_ClearStage(dungeonInfo, stage);
            await _itemService.UpdateItems(items);
        }
    }
}