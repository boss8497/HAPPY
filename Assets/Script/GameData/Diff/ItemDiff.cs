using System;
using System.Linq;
using Expression;
using Script.GameData.Data.Interface;
using Script.GameData.Model;
using Script.GameInfo;
using Script.GameInfo.Info.Enum;
using Script.GameInfo.Item;
using Script.GameInfo.Table;

namespace Script.GameData.Diff {
    [System.Serializable]
    public struct ItemDiff {
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

        public ItemDiff(ItemModel model) : this() {
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

        public ItemDiff(IItemData data) {
            uid      = data.ItemUid.CurrentValue;
            groupUid = data.GroupUid.CurrentValue;
            infoUid  = data.ItemInfoUid.CurrentValue;
            count    = data.Count.CurrentValue;
            level    = data.Level.CurrentValue;
            grade    = data.Grade.CurrentValue;
            tier     = data.Tier.CurrentValue;
            exp      = data.Exp.CurrentValue;    // IItemData에는 exp 배열이 없으므로 기본값으로 설정
            expMax   = data.ExpMax.CurrentValue; // IItemData에는 expMax 배열이 없으므로 기본값으로 설정
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
            exp      = data.Exp.CurrentValue;    // IItemData에는 exp 배열이 없으므로 기본값으로 설정
            expMax   = data.ExpMax.CurrentValue; // IItemData에는 expMax 배열이 없으므로 기본값으로 설정
        }

        /// <summary>
        /// Current - Other
        /// </summary>
        /// <param name="other"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private bool Diff(ItemDiff other, out ItemDiffResult result) {
            result = new();
            if (uid != other.uid || groupUid != other.groupUid || infoUid != other.infoUid) {
                result.Result = false;
                return false;
            }

            result.Result       = true;
            result.CountChanged = count - other.count;
            result.LevelChanged = level - other.level;
            result.GradeChanged = grade - other.grade;
            result.TierChanged  = tier - other.tier;
            result.ExpChanged   = new double[(int)LevelType.Max];
            for (int i = 0; i < exp.Length; i++) {
                result.ExpChanged[i] = exp[i] - other.exp[i];
            }

            return true;
        }

        public static ItemDiffResult operator -(ItemDiff a, ItemDiff b) {
            a.Diff(b, out var result);
            return result;
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

    public struct ItemDiffResult {
        public double   CountChanged;
        public double   LevelChanged;
        public double   GradeChanged;
        public double   TierChanged;
        public double[] ExpChanged;

        public bool Result;
    }
}