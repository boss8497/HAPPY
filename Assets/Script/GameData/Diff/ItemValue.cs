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
                Reset();
                return;
            }

            uid      = model.uid;
            groupUid = model.groupUid;
            infoUid  = model.infoUid;
            count    = model.count;
            level    = model.level;
            grade    = model.grade;
            tier     = model.tier;
            exp      = model.exp;

            var itemInfo = GameInfoManager.Instance.Get<ItemInfo>(infoUid);
            if (itemInfo == null) {
                expMax = Array.Empty<double>();
            }
            else {
                using var _ = CreateValueContext(model.level, model.grade, model.tier);
                expMax = new double[(int)LevelType.Max];

                foreach (var expInfo in itemInfo.expUids.Select(s => GameInfoManager.Instance.Get<ExpInfo>(s))) {
                    expMax[(int)expInfo.levelType] = expInfo.Calc();
                }
            }
        }

        public ItemValue(IItemData data) : this() {
            if (data == null) {
                Reset();
                return;
            }

            uid      = data.ItemUid.CurrentValue;
            groupUid = data.GroupUid.CurrentValue;
            infoUid  = data.ItemInfoUid.CurrentValue;
            count    = data.Count.CurrentValue;
            level    = data.Level.CurrentValue;
            grade    = data.Grade.CurrentValue;
            tier     = data.Tier.CurrentValue;
            exp      = data.Exp.CurrentValue;              // IItemData에는 exp 배열이 없으므로 기본값으로 설정
            expMax   = data.ExpMax.CurrentValue.ToArray(); // IItemData에는 expMax 배열이 없으므로 기본값으로 설정
        }

        private void Reset() {
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
            exp      = model.exp;

            var itemInfo = GameInfoManager.Instance.Get<ItemInfo>(infoUid);
            if (itemInfo == null) {
                expMax = Array.Empty<double>();
            }
            else {
                using var _ = CreateValueContext(model.level, model.grade, model.tier);
                expMax = itemInfo.expUids.Select(s => GameInfoManager.Instance.Get<ExpInfo>(s))
                                 .Select(s => s.Calc())
                                 .ToArray();
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
            exp      = data.Exp.CurrentValue;              // IItemData에는 exp 배열이 없으므로 기본값으로 설정
            expMax   = data.ExpMax.CurrentValue.ToArray(); // IItemData에는 expMax 배열이 없으므로 기본값으로 설정
        }

        /// <summary>
        /// Current - Other
        /// </summary>
        /// <param name="other"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private DiffResult Diff(ItemValue other) {
            var result = new DiffResult();
            if (uid != other.uid || groupUid != other.groupUid || infoUid != other.infoUid) {
                result.Result = false;
                return result;
            }

            result.ItemUid     = uid;
            result.ItemInfoUid = infoUid;

            result.Result         = true;
            result.CountChanged   = count - other.count;
            result.LevelChanged   = level - other.level;
            result.GradeChanged   = grade - other.grade;
            result.TierChanged    = tier - other.tier;
            result.PreviousExp    = exp;
            result.PreviousMaxExp = expMax;
            result.BeforeExp      = other.exp;
            result.BeforeMaxExp   = other.expMax;

            return result;
        }


        public static DiffResult operator -(ItemModel[] models, ItemValue b) {
            var sameItem = models.FirstOrDefault(r => r.uid == b.uid);
            if (sameItem != null) {
                var a = new ItemValue(sameItem);
                return a.Diff(b);
            }

            return new();
        }

        public static DiffResult operator -(ItemValue a, ItemValue b) {
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

    public class DiffResult : IScreenOption {
        public long ItemUid;
        public int  ItemInfoUid;

        public double CountChanged;
        public double LevelChanged;
        public double GradeChanged;
        public double TierChanged;


        public double[] PreviousExp;
        public double[] PreviousMaxExp;

        public double[] BeforeExp;
        public double[] BeforeMaxExp;

        public bool Result;
    }

    public class DiffResultList : IScreenOption {
        public List<DiffResult> Results;
    }
}