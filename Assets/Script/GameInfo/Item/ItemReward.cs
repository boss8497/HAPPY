using System;
using Script.GameInfo.Attribute;

namespace Script.GameInfo.Item {
    [Serializable]
    public class ItemReward {
        [Item]
        public int itemUid;

        public double count;
        public int    level;
        public int    grade;
        public int    tier;
    }
}