using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.DataBase.Enum;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;

namespace Script.Client {
    /// <summary>
    /// 실제로 Server와 통신하는것 처럼 코딩 하자.
    /// </summary>
    public partial class GameClient : IClient {
        private readonly string    _groupPath = $"{nameof(GroupModel)}.json";
        
        public async UniTask<GroupModel> Req_Group() {
            GroupModel CreateGroupModel() {
                var groupModel = new GroupModel();
                // 일단 uid는 1로 설정
                groupModel.uid = 1;

                var dungeonInfo = GameInfoManager.Instance.Get<DungeonInfo>(GameInfoManager.Instance.Config.startDungeon);
                if (dungeonInfo == null) {
                    throw new Exception($"시작 던전 정보가 없습니다. DungeonId: {GameInfoManager.Instance?.Config?.startDungeon}");
                }
            
                groupModel.dungeonProgresses = new []{
                    new DungeonProgress {
                        dungeonUid = dungeonInfo.UID,
                        stageGuid  = dungeonInfo.stages?.FirstOrDefault()?.guid.Value ?? Guid.Empty,
                        cleared    = false,
                        category   = (int)dungeonInfo.category,
                    }
                };
                return groupModel;
            }
            
            async UniTask<GroupModel> Load() {
                return await _dataBase.LoadAsync<GroupModel>(_groupPath, DataType.Json);
            }
            await UniTask.WaitUntil(() => _dataBase.Initialized);

            //첫 접속
            if (_dataBase.Exists(_groupPath)) {
                return await Load();
            }

            var model = CreateGroupModel();
            await Req_SaveGroup(model);
            
            return model;
        }

        public async UniTask Req_SaveGroup(GroupModel model) {
            await _dataBase.SaveAsync(_groupPath, model, DataType.Json);
        }
        
    }
}