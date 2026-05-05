using System;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Script.GameInfo.Enum;
using Script.GameInfo.Item;
using Script.GameInfo.Table;
using UnityEditor.Android;

namespace Script.GameData.Model {
    [MessagePackObject]
    public partial class ItemModel {
        // DB에 사용하는 uid
        [Key(0)] public long uid;

        // 아이템이 속한 GroupModel의 uid
        [Key(1)] public long groupUid;

        // 아이템 정보의 uid
        [Key(2)] public int infoUid;

        [Key(3)] public double count;
        [Key(4)] public int    level;
        [Key(5)] public int    grade;
        [Key(6)] public int    tier;

        [IgnoreMember, JsonIgnore]
        public ItemInfo ItemInfo => GameInfoManager.Instance.Get<ItemInfo>(infoUid);

        public bool SameItem(ItemModel other) {
            var itemInfo = ItemInfo;

            return (itemInfo.flag.HasFlag(ItemFlag.IgnoreLevelEqual) || level == other.level)
                && (itemInfo.flag.HasFlag(ItemFlag.IgnoreGradeEqual) || grade == other.grade)
                && (itemInfo.flag.HasFlag(ItemFlag.IgnoreTierEqual) || tier == other.tier);
        }
        
        public bool SameItem(int otherLevel, int otherGrade, int otherTier) {
            var itemInfo = ItemInfo;

            return  (itemInfo.flag.HasFlag(ItemFlag.IgnoreLevelEqual) || level == otherLevel)
                && (itemInfo.flag.HasFlag(ItemFlag.IgnoreGradeEqual) || grade == otherGrade)
                && (itemInfo.flag.HasFlag(ItemFlag.IgnoreTierEqual) || tier == otherTier);
        }
    }


    [MessagePackObject]
    public partial class ItemModelTable {
        [Key(0)] public List<ItemModel> items   = new();
        [Key(1)] public long            lastUid = 1; // 아이템이 추가될때 마다 증가
    }
}