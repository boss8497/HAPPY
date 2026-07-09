using System;
using MessagePack;

namespace Script.GameData.Model {
    /// <summary>
    /// 서버에서 받은 보상 및 업데이트 아이템
    /// </summary>
    [MessagePackObject]
    public partial class ItemSyncModel {
        // 보상받은 rewardInfo는 모두 넣자
        [Key(0)] public int[] rewardInfoUids = Array.Empty<int>();

        // Update된 아이템 ( level, grade, tier, count, exp 등.. 변경이 있으면 모두 넣고 Client의 ItemService에서 Update )
        [Key(1)] public ItemModel[] updateItems = Array.Empty<ItemModel>();
    }
}