using Cysharp.Threading.Tasks;
using R3;
using Script.GameData.Data;
using Script.GameInfo.Info.Enum;

namespace Script.GamePlay.Service.Interface {
    public interface IItemService : IService {
        ReactiveProperty<int>      SubscribeItemInfoUid(int infoUid);
        ReactiveProperty<ItemData> GetItem(int              infoUid);
        ReactiveProperty<ItemData> GetItem(long             itemUid);
        UniTask                    ItemLevelUp(ItemData     item, LevelType type);
    }
}