using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Info.Enum;

namespace Script.Client {
    public interface IClient {
        UniTask<GroupModel>  Req_Group();
        UniTask              Req_SaveGroup(GroupModel     model);
        UniTask<ItemModel[]> Req_Inventory(long           groupUid);
        UniTask<ItemModel>   Req_ItemLevelUp(ItemModel    model,       LevelType type);
        UniTask<bool>        Req_EnterDungeon(DungeonInfo dungeonInfo, Stage     stage);
        UniTask              Req_RemoveGroup();
        UniTask<ItemModel[]> Req_ClearStage(DungeonInfo dungeonInfo, Stage stage);
    }
}