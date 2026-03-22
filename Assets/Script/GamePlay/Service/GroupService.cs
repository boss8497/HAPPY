using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.DataBase.Enum;
using Script.DataBase.Interface;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Service {
    public class GroupService : IGroupService {
        private readonly IDataBase _dataBase;
        private readonly string    _path = $"{nameof(GroupData)}.json";

        private GroupData _groupData;
        public  GroupData GroupData => _groupData;

        public bool Initialized { get; private set; }


        public GroupService(IDataBase dataBase) {
            _dataBase = dataBase;
            Initialize().Forget();
        }

        private async UniTaskVoid Initialize() {
            await UniTask.WaitUntil(() => _dataBase.Initialized);

            //첫 접속
            if (_dataBase.Exists(_path)) {
                await Load();
            }
            else {
                _groupData = CreateGroupData();
                await Save();
            }
            
            Initialized = true;
        }

        private async UniTask Save() {
            await _dataBase.SaveAsync(_path, _groupData, DataType.Json);
        }
        private async UniTask Load() {
            _groupData = await _dataBase.LoadAsync<GroupData>(_path, DataType.Json);    
        }

        private GroupData CreateGroupData() {
            var groupData = new GroupData();

            var dungeonInfo = GameInfoManager.Instance.Get<DungeonInfo>(GameInfoManager.Instance.Config.startDungeon);
            if (dungeonInfo == null) {
                throw new Exception($"시작 던전 정보가 없습니다. DungeonId: {GameInfoManager.Instance?.Config?.startDungeon}");
            }
            
            groupData.dungeonProgresses = new []{
                new DungeonProgress {
                    dungeonUid = dungeonInfo.UID,
                    stageGuid = dungeonInfo.stages?.FirstOrDefault()?.guid.Value ?? Guid.Empty,
                    cleared   = false,
                    category = (int)dungeonInfo.category,
                }
            };
            
            return groupData;
        }

        public DungeonProgress GetDungeon(Category dungeonCategory) {
            var category = (int)dungeonCategory;
            return _groupData.dungeonProgresses?.FirstOrDefault(r => r.category == category);
        }

        public async UniTask ClearedDungeon(Category dungeonCategory) {
            var dungeonProgress = GetDungeon(dungeonCategory);
            if (dungeonProgress == null) {
                Debug.LogError($"Not found Category: {dungeonCategory.ToString()}");
                return;
            }
            
            var category    = (int)dungeonCategory;
            var dungeonInfo =  GameInfoManager.Instance.Get<DungeonInfo>(dungeonProgress.dungeonUid);
            var index       = _groupData.dungeonProgresses.FindIndex(r => r.category == category);
            if (index == -1) {
                Debug.LogError($"Not found Dungeon {dungeonProgress.dungeonUid}");
                return;
            }
            
            
            if (dungeonInfo.IsLastStage(dungeonProgress.stageGuid)) {
                if (dungeonInfo.IsLastDungeon()) {
                    _groupData.dungeonProgresses[index].cleared = true;
                }
                else {
                    var nextDungeonInfo = GameInfoManager.Instance.Get<DungeonInfo>(dungeonInfo.nextDungeonUid);
                    if (nextDungeonInfo == null) {
                        _groupData.dungeonProgresses[index].cleared = true;
                    }
                    else {
                        _groupData.dungeonProgresses[index].dungeonUid = nextDungeonInfo.UID;
                        _groupData.dungeonProgresses[index].stageGuid  = nextDungeonInfo.stages?.FirstOrDefault()?.guid.Value ?? Guid.Empty;
                        _groupData.dungeonProgresses[index].cleared    = false;
                        
                    }
                }
            }
            else {
                var nextDungeon = dungeonInfo.NextStage(dungeonProgress.stageGuid);
                if (nextDungeon == null) {
                    Debug.LogError($"Not found Next Stage: {dungeonCategory.ToString()}:{dungeonProgress.dungeonUid}");
                    return;
                }
                
                _groupData.dungeonProgresses[index].stageGuid = nextDungeon.guid.Value;
                _groupData.dungeonProgresses[index].cleared   = false;
            }
            
            await Save();
            
        }
        
        
    }
}