using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Expression;
using Script.DataBase.Enum;
using Script.GameData.Model;
using Script.GameInfo;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Enum;
using Script.GameInfo.Info;
using Script.GameInfo.Info.Enum;
using Script.GameInfo.Item;
using Script.GameInfo.Table;
using Script.Utility.Runtime;
using Sirenix.Utilities;

namespace Script.Client {
    /// <summary>
    /// 실제로 Server와 통신하는것 처럼 코딩 하자.
    /// </summary>
    public partial class GameClient : IClient {
        private readonly string _groupPath = $"{nameof(GroupModel)}.json";

        private GroupModel _groupModel;
        private long       _playCharacterUid;

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
                _groupModel = await Load();
                return _groupModel;
            }

            _groupModel = CreateGroup();
            await Req_SaveGroup(_groupModel);
            await _dataBase.SaveItemTable();

            return _groupModel;
        }

        public async UniTask Req_SaveGroup(GroupModel model, CancellationToken ct = default) {
            await _dataBase.SaveAsync(_groupPath, model, DataType.Json);
        }

        public async UniTask<ItemModel[]> Req_Inventory(long groupUid) {
            return await _dataBase.GetInventory(groupUid);
        }

        public async UniTask<ItemModel> Req_GetItem(long groupUid, long itemUid) {
            return await _dataBase.GetItem(groupUid, itemUid);
        }

        public async UniTask<ItemModel> Req_ItemLevelUp(ItemModel model, LevelType type) {
            return await _dataBase.LevelUpItem(model, type);
        }

        public UniTask<bool> Req_EnterDungeon(DungeonInfo dungeonInfo, Stage stage, long characterUid) {
            _playCharacterUid = characterUid;
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

        public async UniTask<ItemSyncModel> Req_ClearStage(DungeonInfo dungeonInfo, Stage stage, long characterUid) {
            var dungeonCategory = dungeonInfo.category;
            var dungeonProgress = GetDungeon(dungeonCategory);

            if (dungeonProgress == null) {
                throw new Exception($"Not found Category: {(Category)dungeonInfo.category}");
            }

            var stageIndex   = dungeonInfo.stages.FindIndex(r => r.guid.Value == stage.guid.Value);
            var clearedIndex = dungeonInfo.stages.FindIndex(r => r.guid.Value == dungeonProgress.stageGuid);

            // 이 전 스테이지 클리어 했기 대문에 저장할게 없음
            // clearedIndex = -1 이라면 아마 이전 던전의 Stage일 가능성이 있음. 아니면 해킹 및 버그로 다음 던전 사용이기 때문에 return
            if (clearedIndex < 0 || clearedIndex < stageIndex) {
                throw new Exception($"clearedIndex = -1 이라면 아마 이전 던전의 Stage일 가능성이 있음. 아니면 해킹 및 버그로 다음 던전 사용이기 때문에 return");
            }

            if (dungeonProgress.cleared == false && clearedIndex < stageIndex) {
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

            var serverItemSync = await Req_StageRewards(_groupModel.uid, dungeonInfo.UID, stage.guid.Value, _playCharacterUid);
            
            await Req_SaveGroup(_groupModel);
            await _dataBase.SaveItemTable();

            return serverItemSync;
        }

        private async UniTask Req_CharacterExpUp(long groupUid, long itemUid, int expItemInfoUid, double count, LevelType levelType) {
            var characterItem = await _dataBase.GetItem(groupUid, itemUid);
            if (characterItem == null) {
                throw new Exception($"Not found Character Item: {itemUid}");
            }

            var characterItemInfo = characterItem.ItemInfo;
            if (characterItemInfo == null) {
                throw new Exception($"Not found Character ItemInfo: {itemUid}");
            }

            if (characterItemInfo.expUids.Length <= 0) {
                throw new Exception($"Character ItemInfo doesn't have expUids: {itemUid}");
            }

            var expInfoUid = characterItemInfo.expUids.FirstOrDefault(r => {
                var info = GameInfoManager.Instance.Get<ExpInfo>(r);
                return info.levelType == levelType && info.itemUid == expItemInfoUid;
            });

            if (expInfoUid <= 0) {
                throw new Exception($"Character ItemInfo doesn't have expUids: {itemUid}");
            }

            var       expInfo = GameInfoManager.Instance.Get<ExpInfo>(expInfoUid);
            using var _       = CreateValueContext(characterItem);

            var currentExp = characterItem.exp[(int)levelType];
            var limitExp   = expInfo.Calc();

            if (currentExp + count < limitExp) {
                // 경험치만 추가    
            }
            else {
                // 레벨업 필요
            }
        }

        private UniTask<ItemModel[]> Req_AddRewards(long groupModelUid, int[] stageRewards) {
            return UniTask.FromResult(_dataBase.AddRewards(groupModelUid, stageRewards));
        }

        private async UniTask<ItemSyncModel> Req_StageRewards(long groupModelUid, int dungeonUid, Guid stageGuid, long characterUid) {
            return await _dataBase.StageRewards(groupModelUid, dungeonUid, stageGuid, characterUid);
        }

        public DungeonProgress GetDungeon(Category dungeonCategory) {
            var category = (int)dungeonCategory;
            return _groupModel.dungeonProgresses?.FirstOrDefault(r => r.category == category);
        }

        public async UniTask<(GroupModel model, bool result)> Req_UpdateTutorial(TutorialProgress progress, CancellationToken ct = default) {
            if ((int)_groupModel.tutorialProgress + 1 != (int)progress) {
                return (_groupModel, false);
            }
            _groupModel.tutorialProgress = progress;
            await Req_SaveGroup(_groupModel, ct);
            return (_groupModel, true);
        }


        //

        private ValueContext CreateValueContext(
            ItemModel item,
            int       levelOffset = 0,
            int       gradeOffset = 0,
            int       tierOffset  = 0
        ) {
            return new(
                new ValueProvider()
                    .Add("level", (item?.level ?? 1) + levelOffset)
                    .Add("grade", (item?.grade ?? 0) + gradeOffset)
                    .Add("tier", (item?.tier ?? 0) + tierOffset)
            );
        }
    }
}