using Cysharp.Threading.Tasks;
using Script.DataBase.Enum;
using Script.GameData.Model;
using Script.GameInfo.Info.Enum;
using Script.GameInfo.Item;

namespace Script.DataBase.Interface {
    /// <summary>
    /// 유저 데이터를 저장할 인터페이스
    /// 현재는 따로 서버 구현 예정이 없기 때문에 json 으로 저장 목표
    /// 테스트 시 json으로 저장하고 나중에는 MessagePack으로 변경할 예정
    /// </summary>
    public interface IDataBase {
        bool Initialized { get; }

        UniTask<T>    LoadAsync<T>(string path, DataType type                = DataType.Json);
        UniTask       SaveAsync<T>(string path, T        data, DataType type = DataType.Json);
        bool          Exists(string       path);
        UniTask<bool> DeleteAsync(string  path);


        ItemModel AddItem(
            long   groupUid,
            int    itemUid,
            double count = 1d,
            int    level = 1,
            int    grade = 0,
            int    tier  = 0
        );

        ItemModel AddItem(
            long       groupUid,
            ItemReward reward
        );

        ItemModel[] AddRewards(
            long  groupUid,
            int[] rewardInfoUids
        );

        UniTask<ItemModel[]> GetInventory(long groupUid);
        UniTask              SaveItemTable();
        UniTask<ItemModel>   GetItem(long          groupUid, long      itemUid);
        UniTask<ItemModel>   CharacterExpUp(long   groupUid, long      itemUid, int expItemInfoUid, double count, LevelType levelType);
        UniTask<ItemModel>   LevelUpItem(ItemModel item,     LevelType levelType);
        UniTask              RemoveGroupItems(long groupUid);
    }
}