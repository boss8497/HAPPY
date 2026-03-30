using System;
using System.Linq;
using Cysharp.Threading.Tasks;
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
        private readonly IDataBase _dataBase;
        private readonly string    _path = $"{nameof(GroupModel)}.json";

        private GroupData _groupData;
        public  IGroupData GroupData => _groupData;

        public bool Initialized { get; private set; }


        public GroupService(IDataBase dataBase) {
            _dataBase = dataBase;
        }

        public void Initialize() {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync() {
            await UniTask.WaitUntil(() => _dataBase.Initialized);

            //첫 접속
            if (_dataBase.Exists(_path)) {
                _groupData = await Load();
            }
            else {
                _groupData = CreateGroupData();
                await Save();
            }
            
            Initialized = true;
        }

        private async UniTask Save() {
            await _dataBase.SaveAsync(_path, _groupData.Model.CurrentValue, DataType.Json);
        }
        
        private async UniTask<GroupData> Load() {
            var model = await _dataBase.LoadAsync<GroupModel>(_path, DataType.Json);
            return new(model);
        }

        private GroupData CreateGroupData() {
            var groupModel = new GroupModel();

            var dungeonInfo = GameInfoManager.Instance.Get<DungeonInfo>(GameInfoManager.Instance.Config.startDungeon);
            if (dungeonInfo == null) {
                throw new Exception($"시작 던전 정보가 없습니다. DungeonId: {GameInfoManager.Instance?.Config?.startDungeon}");
            }
            
            groupModel.dungeonProgresses = new []{
                new DungeonProgress {
                    dungeonUid = dungeonInfo.UID,
                    stageGuid = dungeonInfo.stages?.FirstOrDefault()?.guid.Value ?? Guid.Empty,
                    cleared   = false,
                    category = (int)dungeonInfo.category,
                }
            };
            
            return new(groupModel);
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
            var dungeonInfo =  GameInfoManager.Instance.Get<DungeonInfo>(dungeonProgress.dungeonUid);
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
            
            await Save();
            
        }
    }
}