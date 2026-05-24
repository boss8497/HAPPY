using System;
using System.Linq;
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
                return await Load();
            }

            var model = CreateGroup();
            await Req_SaveGroup(model);
            await _dataBase.SaveItemTable();
            
            return model;
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
    }
}