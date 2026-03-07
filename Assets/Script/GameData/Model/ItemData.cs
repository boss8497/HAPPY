using MessagePack;

namespace Script.GameData.Model {
    [MessagePackObject]
    public partial class ItemData {
        //DB에 사용하는 uid
        [Key(0)] public long uid;

        //아이템 정보의 uid
        [Key(1)] public long   infoUid;
        [Key(2)] public double count;
    }
}