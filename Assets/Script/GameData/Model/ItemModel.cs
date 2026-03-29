using MessagePack;

namespace Script.GameData.Model {
    [MessagePackObject]
    public partial class ItemModel {
        //DB에 사용하는 uid
        [Key(0)] public long uid;

        //아이템 정보의 uid
        [Key(1)] public int    infoUid;
        [Key(2)] public double count;

        [Key(3)] public int level;
        [Key(4)] public int grade;
        [Key(5)] public int tier;
    }
}