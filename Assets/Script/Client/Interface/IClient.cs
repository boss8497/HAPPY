using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Info.Enum;

namespace Script.Client {
    public interface IClient {
        UniTask<GroupModel>  Req_Group();
        UniTask              Req_SaveGroup(GroupModel  model);
        UniTask<ItemModel[]> Req_Inventory(long        groupUid);
        UniTask<ItemModel>   Req_ItemLevelUp(ItemModel model, LevelType type);
    }
}