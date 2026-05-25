using System;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Script.DataBase.Enum;
using Script.GameData.Model;
using Script.GameInfo;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Info.Enum;
using Script.GameInfo.Table;
using Script.Utility.Runtime;

namespace Script.Client {
    /// <summary>
    /// 실제로 Server와 통신하는것 처럼 코딩 하자.
    /// </summary>
    public partial class GameClient : IClient {
        private readonly string    _groupPath = $"{nameof(GroupModel)}.json";
        
        private GroupModel _groupModel;
        
        public async UniTask<GroupModel> Req_Group() {
            GroupModel CreateGroup() {
                var groupModel = new GroupModel();
                // 일단 uid는 1로 설정
                groupModel.uid = 1;

                var dungeonProgress = ListPool.Get<DungeonProgress>();
                foreach (var dungeonInfo in GameInfoManager.Instance.GetCollection<DungeonInfo>()) {
                    // 던전 정보 생성
                    dungeonProgress.Add(new DungeonProgress {
                        dungeonUid = dungeonInfo.UID,
                        stageGuid  = dungeonInfo.stages?.FirstOrDefault()?.guid.Value ?? Guid.Empty,
                        cleared    = false,
                        category   = (int)dungeonInfo.category,
                    });
                }
                groupModel.dungeonProgresses = dungeonProgress.ToArray();
                ListPool.Return(dungeonProgress);

                var gameConfiguration = GameInfoManager.Instance.Config;
                var startItems        = GameInfoManager.Instance.Get<RewardInfo>(gameConfiguration.startItems);
                foreach (var startItem in startItems.itemRewards) {
                    _dataBase.AddItem(groupModel.uid, startItem);
                }
                
                return groupModel;
            }
            
            async UniTask<GroupModel> Load() {
                return await _dataBase.LoadAsync<GroupModel>(_groupPath, DataType.Json);
            }
            await UniTask.WaitUntil(() => _dataBase.Initialized);

            //첫 접속 확인
            if (_dataBase.Exists(_groupPath)) {
                _groupModel =  await Load();
                return _groupModel;
            }

            _groupModel = CreateGroup();
            await Req_SaveGroup(_groupModel);
            await _dataBase.SaveItemTable();
            
            return _groupModel;
        }

        public async UniTask Req_SaveGroup(GroupModel model) {
            await _dataBase.SaveAsync(_groupPath, model, DataType.Json);
        }

        public async UniTask<ItemModel[]> Req_Inventory(long groupUid) {
            return await _dataBase.GetInventory(groupUid);
        }

        public async UniTask<ItemModel> Req_ItemLevelUp(ItemModel model, LevelType type) {
            return await _dataBase.LevelUpItem(model, type);
        }

        public UniTask<bool> Req_EnterDungeon(DungeonInfo dungeonInfo, Stage stage) {
            return UniTask.FromResult(true);
        }

        public async UniTask Req_RemoveGroup() {
            async UniTask<GroupModel> Load() {
                return await _dataBase.LoadAsync<GroupModel>(_groupPath, DataType.Json);
            }
            
            if (_dataBase.Exists(_groupPath)) {
                var group = await Load();
                await _dataBase.RemoveGroupItems(group.uid);
                await _dataBase.DeleteAsync(_groupPath);
            }
        }

        public async UniTask<ItemModel[]> Req_ClearStage(DungeonInfo dungeonInfo, Stage stage) {
            var  rewards         = Array.Empty<ItemModel>();
             var dungeonCategory = dungeonInfo.category;
            var  dungeonProgress = GetDungeon(dungeonCategory);

            if (dungeonProgress == null) {
                throw new Exception($"Not found Category: {(Category)dungeonInfo.category}");
            }

            var stageIndex   = dungeonInfo.stages.FindIndex(r => r.guid.Value == stage.guid.Value);
            var clearedIndex = dungeonInfo.stages.FindIndex(r => r.guid.Value == dungeonProgress.stageGuid);

            // 이 전 스테이지 클리어 했기 대문에 저장할게 없음
            // clearedIndex = -1 이라면 아마 이전 던전의 Stage일 가능성이 있음. 아니면 해킹 및 버그로 다음 던전 사용이기 때문에 return
            if (clearedIndex < 0 || clearedIndex < stageIndex) return rewards;

            if (dungeonProgress.cleared == false && clearedIndex > stageIndex) {
                throw new Exception($"이 전 스테이지를 클리어하지 않고 먼저 스테이지를 클리어할 수 없습니다.");
            }


            // 여기서 부터는 clearedIndex == stageIndex로 설정됨 아닌 부분은 위에서 다 return 했음
            var category      = (int)dungeonCategory;
            var index         = _groupModel.dungeonProgresses.FindIndex(r => r.category == category);
            var isLastStage   = dungeonInfo.IsLastStage(dungeonProgress.stageGuid);
            var isLastDungeon = dungeonInfo.IsLastDungeon();


            // 아직 스테이지가 남았으면 다음 스테이지 guid 설정 하고 cleared를 false로 설정
            if (isLastStage) {
                // 마지막 던전이면 clear 설정
                if (isLastDungeon) {
                    _groupModel.dungeonProgresses[index].cleared = true;
                }
                else {
                    var nextDungeonInfo = GameInfoManager.Instance.Get<DungeonInfo>(dungeonInfo.nextDungeonUid);
                    if (nextDungeonInfo == null) {
                        _groupModel.dungeonProgresses[index].cleared = true;
                    }
                    else {
                        _groupModel.dungeonProgresses[index].dungeonUid = nextDungeonInfo.UID;
                        _groupModel.dungeonProgresses[index].stageGuid  = nextDungeonInfo.stages?.FirstOrDefault()?.guid.Value ?? Guid.Empty;
                        _groupModel.dungeonProgresses[index].cleared    = false;
                    }
                }
            }
            else {
                _groupModel.dungeonProgresses[index].stageGuid = dungeonInfo.stages[++stageIndex].guid.Value;
                _groupModel.dungeonProgresses[index].cleared   = false;
            }

            rewards = await Req_AddRewards(_groupModel.uid, stage.rewards);
            await Req_SaveGroup(_groupModel);
            await _dataBase.SaveItemTable();
            
            return rewards;
        }

        private UniTask<ItemModel[]> Req_AddRewards(long groupModelUid, int[] stageRewards) {
            return UniTask.FromResult(_dataBase.AddRewards(groupModelUid, stageRewards));
        }


        public DungeonProgress GetDungeon(Category dungeonCategory) {
            var category = (int)dungeonCategory;
            return _groupModel.dungeonProgresses?.FirstOrDefault(r => r.category == category);
        }
    }
}