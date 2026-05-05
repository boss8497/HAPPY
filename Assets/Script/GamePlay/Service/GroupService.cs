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
using Script.GamePlay.Service.Interface;
using Script.Utility.Runtime;
using UnityEngine;
using VContainer.Unity;

namespace Script.GamePlay.Service {
    public class GroupService : IGroupService, IInitializable {
        private readonly IClient _client;

        private GroupData  _groupData;
        public  IGroupData GroupData => _groupData;

        public bool Initialized { get; private set; }


        public GroupService(
            IClient client
        ) {
            _client = client;
        }

        public void Initialize() {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync() {
            //첫 접속
            var model = await _client.Req_Group();
            _groupData = new GroupData(model);
            Initialized = true;
        }

        public DungeonProgress GetDungeon(Category dungeonCategory) {
            var category = (int)dungeonCategory;
            return _groupData.Model.CurrentValue.dungeonProgresses?.FirstOrDefault(r => r.category == category);
        }

        public async UniTask ClearedDungeon(Category dungeonCategory) {
            var dungeonProgress = GetDungeon(dungeonCategory);
            if (dungeonProgress == null) {
                Debug.LogError($"Not found Category: {dungeonCategory.ToString()}");
                return;
            }

            var category    = (int)dungeonCategory;
            var dungeonInfo = GameInfoManager.Instance.Get<DungeonInfo>(dungeonProgress.dungeonUid);
            var index       = _groupData.Model.CurrentValue.dungeonProgresses.FindIndex(r => r.category == category);
            if (index == -1) {
                Debug.LogError($"Not found Dungeon {dungeonProgress.dungeonUid}");
                return;
            }


            if (dungeonInfo.IsLastStage(dungeonProgress.stageGuid)) {
                if (dungeonInfo.IsLastDungeon()) {
                    _groupData.Model.CurrentValue.dungeonProgresses[index].cleared = true;
                }
                else {
                    var nextDungeonInfo = GameInfoManager.Instance.Get<DungeonInfo>(dungeonInfo.nextDungeonUid);
                    if (nextDungeonInfo == null) {
                        _groupData.Model.CurrentValue.dungeonProgresses[index].cleared = true;
                    }
                    else {
                        _groupData.Model.CurrentValue.dungeonProgresses[index].dungeonUid = nextDungeonInfo.UID;
                        _groupData.Model.CurrentValue.dungeonProgresses[index].stageGuid  = nextDungeonInfo.stages?.FirstOrDefault()?.guid.Value ?? Guid.Empty;
                        _groupData.Model.CurrentValue.dungeonProgresses[index].cleared    = false;
                    }
                }
            }
            else {
                var nextDungeon = dungeonInfo.NextStage(dungeonProgress.stageGuid);
                if (nextDungeon == null) {
                    Debug.LogError($"Not found Next Stage: {dungeonCategory.ToString()}:{dungeonProgress.dungeonUid}");
                    return;
                }

                _groupData.Model.CurrentValue.dungeonProgresses[index].stageGuid = nextDungeon.guid.Value;
                _groupData.Model.CurrentValue.dungeonProgresses[index].cleared   = false;
            }

            await _client.Req_SaveGroup(_groupData.Model.CurrentValue);
        }
    }
}