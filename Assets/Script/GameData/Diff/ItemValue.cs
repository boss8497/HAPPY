using System;
using System.Collections.Generic;
using System.Linq;
using Expression;
using Script.GameData.Data.Interface;
using Script.GameData.Model;
using Script.GameInfo;
using Script.GameInfo.Info.Enum;
using Script.GameInfo.Item;
using Script.GameInfo.Table;
using Script.GUI.ScreenData.Interface;

namespace Script.GameData.Diff {
    /// <summary>
    /// 아이템의 기본 정보를 값으로 들고 있는 Class
    /// </summary>
    [System.Serializable]
    public struct ItemValue {
        // DB에 사용하는 uid
        public long uid;

        // 아이템이 속한 GroupModel의 uid
        public long groupUid;

        // 아이템 정보의 uid
        public int infoUid;

        public double   count;
        public int      level;
        public int      grade;
        public int      tier;
        public double[] exp;
        public double[] expMax;

        public bool Valid => uid > 0 && infoUid > 0;

        public ItemValue(ItemModel model) : this() {
            if (model == null) {
                Release();
                return;
            }

            uid      = model.uid;
            groupUid = model.groupUid;
            infoUid  = model.infoUid;
            count    = model.count;
            level    = model.level;
            grade    = model.grade;
            tier     = model.tier;
            exp      = model.exp.ToArray();
            expMax   = new double[(int)LevelType.Max];

            var itemInfo = GameInfoManager.Instance.Get<ItemInfo>(infoUid);
            if (itemInfo != null) {
                using var _ = CreateValueContext(model.level, model.grade, model.tier);
                foreach (var expInfo in itemInfo.expUids.Select(s => GameInfoManager.Instance.Get<ExpInfo>(s))) {
                    expMax[(int)expInfo.levelType] = expInfo.Calc();
                }
            }
        }

        public ItemValue(IItemData data) : this() {
            if (data == null) {
                Release();
                return;
            }

            uid      = data.ItemUid.CurrentValue;
            groupUid = data.GroupUid.CurrentValue;
            infoUid  = data.ItemInfoUid.CurrentValue;
            count    = data.Count.CurrentValue;
            level    = data.Level.CurrentValue;
            grade    = data.Grade.CurrentValue;
            tier     = data.Tier.CurrentValue;
            exp      = data.Exp.CurrentValue.ToArray();    // IItemData에는 exp 배열이 없으므로 기본값으로 설정
            expMax   = data.ExpMax.CurrentValue.ToArray(); // IItemData에는 expMax 배열이 없으므로 기본값으로 설정
        }

        public void Release() {
            uid      = 0;
            groupUid = 0;
            infoUid  = 0;
            count    = 0;
            level    = 0;
            grade    = 0;
            tier     = 0;
            exp      = Array.Empty<double>();
            expMax   = Array.Empty<double>();
        }

        public void Set(ItemModel model) {
            uid      = model.uid;
            groupUid = model.groupUid;
            infoUid  = model.infoUid;
            count    = model.count;
            level    = model.level;
            grade    = model.grade;
            tier     = model.tier;

            exp    = model.exp.ToArray();
            expMax = new double[(int)LevelType.Max];

            var itemInfo = GameInfoManager.Instance.Get<ItemInfo>(infoUid);
            if (itemInfo != null) {
                using var _ = CreateValueContext(model.level, model.grade, model.tier);
                foreach (var expInfo in itemInfo.expUids.Select(s => GameInfoManager.Instance.Get<ExpInfo>(s))) {
                    expMax[(int)expInfo.levelType] = expInfo.Calc();
                }
            }
        }

        public void Set(IItemData data) {
            uid      = data.ItemUid.CurrentValue;
            groupUid = data.GroupUid.CurrentValue;
            infoUid  = data.ItemInfoUid.CurrentValue;
            count    = data.Count.CurrentValue;
            level    = data.Level.CurrentValue;
            grade    = data.Grade.CurrentValue;
            tier     = data.Tier.CurrentValue;
            
            exp      = data.Exp.CurrentValue.ToArray();    // IItemData에는 exp 배열이 없으므로 기본값으로 설정
            expMax   = data.ExpMax.CurrentValue.ToArray(); // IItemData에는 expMax 배열이 없으므로 기본값으로 설정
        }

        /// <summary>
        /// Current - Other
        /// </summary>
        /// <param name="other"></param>=
        /// <returns></returns>
        private ItemDiff Diff(ItemValue other) {
            return new(this, other);
        }


        public static ItemDiff operator -(ItemValue itemValue, ItemModel[] models) {
            var sameItem = models.FirstOrDefault(r => r.uid == itemValue.uid);
            if (sameItem != null) {
                var other = new ItemValue(sameItem);
                return itemValue.Diff(other);
            }

            return itemValue.Diff(default);
        }

        public static ItemDiff operator -(ItemValue a, ItemValue b) {
            return a.Diff(b);
        }


        private ValueContext CreateValueContext(
            int level,
            int grade,
            int tier
        ) {
            return new(
                new ValueProvider()
                    .Add("level", level)
                    .Add("grade", grade)
                    .Add("tier", tier)
            );
        }
    }
}