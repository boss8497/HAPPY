using Script.GameInfo.Info.Enum;
using Script.GUI.ScreenData.Interface;

namespace Script.GameData.Diff {
    public class ItemDiff : IScreenOption {
        private ItemValue _previous;
        private ItemValue _before;

        public long ItemUid     => _previous.uid;
        public int  ItemInfoUid => _previous.infoUid;

        public bool IsChangedCount => Valid && DiffCount > 0;
        public bool IsChangedLevel => Valid && DiffLevel > 0;
        public bool IsChangedGrade => Valid && DiffGrade > 0;
        public bool IsChangedTier  => Valid && DiffTier > 0;

        public double DiffCount => _before.count - _previous.count;
        public double DiffLevel => _before.level - _previous.level;
        public double DiffGrade => _before.grade - _previous.grade;
        public double DiffTier  => _before.tier - _previous.tier;


        public double[] PreviousExp    => _previous.exp;
        public double[] PreviousMaxExp => _previous.expMax;

        public double[] BeforeExp    => _before.exp;
        public double[] BeforeMaxExp => _before.expMax;

        public bool Valid { get; private set; }

        public ItemDiff() { }

        public ItemDiff(ItemValue previous, ItemValue before) {
            _previous = previous;
            _before   = before;
            Valid     = _previous.Valid && _before.Valid && _previous.uid == _before.uid && _previous.groupUid == _before.groupUid && _previous.infoUid == _before.infoUid;
        }

        public void Set(ItemValue previous, ItemValue before) {
            _previous = previous;
            _before   = before;
            Valid     = _previous.Valid && _before.Valid && _previous.uid == _before.uid && _previous.groupUid == _before.groupUid && _previous.infoUid == _before.infoUid;
        }

        public double GetDiffExp(LevelType levelType) {
            if (!Valid) {
                return 0;
            }

            var levelTypeIndex = (int)levelType;
            if (levelTypeIndex < 0 || levelTypeIndex >= _previous.exp.Length || levelTypeIndex >= _before.exp.Length) {
                return 0;
            }

            var sumExp = 0d;
            if (IsChangedLevel) {
                var startIndex = _previous.GetLevel(levelType);
                var endIndex   = _before.GetLevel(levelType);

                for (int i = startIndex; i < endIndex; i++) {
                    var levelMaxExp = _previous.GetLevelMaxExp(i, levelType);

                    if (levelMaxExp > 0 && startIndex == _previous.level) {
                        sumExp = levelMaxExp - _previous.exp[levelTypeIndex];
                        continue;
                    }

                    sumExp += levelMaxExp;
                }

                sumExp += _before.exp[levelTypeIndex];
            }
            else {
                sumExp = _before.exp[levelTypeIndex] - _previous.exp[levelTypeIndex];
            }


            return sumExp;
        }
    }
}