using Cysharp.Threading.Tasks;
using Script.GameData.Data;
using Script.GameInfo.Info.Enum;

namespace Script.GamePlay.Service.Interface {
    public interface IItemService : IService {
        ItemData GetItem(int          infoUid);
        UniTask  ItemLevelUp(ItemData item, LevelType type);
    }
}