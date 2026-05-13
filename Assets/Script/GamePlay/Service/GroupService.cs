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
        private readonly ISceneLoader _sceneLoader;

        private GroupData  _groupData;
        public  IGroupData GroupData => _groupData;
        public  long       GroupUid  => _groupData?.Model?.CurrentValue?.uid ?? 0;

        public bool Initialized { get; private set; }


        private Tuple<DungeonInfo, Stage> _enterDungeon;

        public GroupService(
            IClient      client,
            ISceneLoader sceneLoader
        ) {
            _client      = client;
            _sceneLoader = sceneLoader;
        }

        public void Initialize() {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync() {
            //첫 접속
            var model = await _client.Req_Group();
            _groupData  = new GroupData(model);
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
        
        public async UniTask ClearedDungeon(DungeonInfo dungeonInfo, Stage stage) {
            var dungeonCategory = dungeonInfo.category;
            var dungeonProgress = GetDungeon(dungeonCategory);
            
            if (dungeonProgress == null) {
                Debug.LogError($"Not found Category: {dungeonCategory.ToString()}");
                return;
            }
            
            var stageIndex   = dungeonInfo.stages.FindIndex(r => r.guid.Value == stage.guid.Value); 
            var clearedIndex = dungeonInfo.stages.FindIndex(r => r.guid.Value == dungeonProgress.stageGuid);

            // 이 전 스테이지 클리어 했기 대문에 저장할게 없음
            // clearedIndex = -1 이라면 아마 이전 던전의 Stage일 가능성이 있음. 아니면 해킹 및 버그로 다음 던전 사용이기 때문에 return
            if (clearedIndex < 0 || clearedIndex < stageIndex) return;

            if (dungeonProgress.cleared == false && clearedIndex > stageIndex) {
                throw new Exception($"이 전 스테이지를 클리어하지 않고 먼저 스테이지를 클리어할 수 없습니다.");
            }
            
            
            // 여기서 부터는 clearedIndex == stageIndex로 설정됨 아닌 부분은 위에서 다 return 했음
            var category      = (int)dungeonCategory;
            var index         = _groupData.Model.CurrentValue.dungeonProgresses.FindIndex(r => r.category == category);
            var isLastStage   = dungeonInfo.IsLastStage(dungeonProgress.stageGuid);
            var isLastDungeon = dungeonInfo.IsLastDungeon();


            // 아직 스테이지가 남았으면 다음 스테이지 guid 설정 하고 cleared를 false로 설정
            if (isLastStage) {
                // 마지막 던전이면 clear 설정
                if (isLastDungeon) {
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
                _groupData.Model.CurrentValue.dungeonProgresses[index].stageGuid = dungeonInfo.stages[++stageIndex].guid.Value;
                _groupData.Model.CurrentValue.dungeonProgresses[index].cleared   = false;
            }
            await _client.Req_SaveGroup(_groupData.Model.CurrentValue);
        }
    }
}