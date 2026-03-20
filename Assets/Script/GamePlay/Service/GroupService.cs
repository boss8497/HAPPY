using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.DataBase.Enum;
using Script.DataBase.Interface;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;

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
                _groupData = await _dataBase.LoadAsync<GroupData>(_path, DataType.Json);    
            }
            else {
                _groupData = NewGroupData();
                 await _dataBase.SaveAsync(_path, _groupData, DataType.Json);
            }
            
            Initialized = true;
        }

        private GroupData NewGroupData() {
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
                }
            };
            
            return groupData;
        }
    }
}