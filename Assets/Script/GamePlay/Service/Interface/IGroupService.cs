using System;
using Cysharp.Threading.Tasks;
using Script.GameData.Data;
using Script.GameData.Data.Interface;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;

namespace Script.GamePlay.Service.Interface {
    public interface IGroupService : IService {
        IGroupData GroupData { get; }
        long       GroupUid  { get; }

        DungeonProgress           GetDungeon(Category        dungeonCategory);
        bool                      IsCleared(DungeonInfo      dungeonInfo, Stage stage);
        bool                      CanEnterStage(DungeonInfo  dungeonInfo, Stage stage);
        UniTask<ItemSyncModel>    ClearedDungeon(DungeonInfo dungeonInfo, Stage stage);
        UniTask                   EnterDungeon(DungeonInfo   dungeonInfo, Stage stage);
        UniTask                   EnterDungeon(DungeonInfo   dungeonInfo, Stage stage, ItemData character, bool loadScene = true);
        Tuple<DungeonInfo, Stage> GetEnterDungeon();
        ItemData                  GetCharacterItem();
    }
}