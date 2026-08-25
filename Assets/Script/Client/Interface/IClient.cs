using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Info;
using Script.GameInfo.Info.Enum;

namespace Script.Client {
    public interface IClient {
        UniTask<GroupModel>                      Req_Group();
        UniTask                                  Req_SaveGroup(GroupModel     model,    CancellationToken ct = default);
        UniTask<ItemModel[]>                     Req_Inventory(long           groupUid);
        UniTask<ItemModel>                       Req_ItemLevelUp(ItemModel    model,       LevelType type);
        UniTask<bool>                            Req_EnterDungeon(DungeonInfo dungeonInfo, Stage     stage, long characterUid);
        UniTask                                  Req_RemoveGroup();
        UniTask<ItemSyncModel>                   Req_ClearStage(DungeonInfo          dungeonInfo, Stage             stage, long characterUid);
        UniTask<(GroupModel model, bool result)> Req_UpdateTutorial(TutorialProgress progress,    CancellationToken ct = default);
    }
}