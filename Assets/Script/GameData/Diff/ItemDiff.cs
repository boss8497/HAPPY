using Script.GUI.ScreenData.Interface;

namespace Script.GameData.Diff {
    public class ItemDiff : IScreenOption {
        private ItemValue _previous;
        private ItemValue _before;

        public long ItemUid     => _previous.uid;
        public int  ItemInfoUid => _previous.infoUid;
        
        public bool IsChangedCount => DiffCount > 0;
        public bool IsChangedLevel => DiffLevel > 0;
        public bool IsChangedGrade => DiffGrade > 0;
        public bool IsChangedTier => DiffTier > 0;
        
        public double DiffCount => _previous.count - _before.count;
        public double DiffLevel => _previous.level - _before.level;
        public double DiffGrade => _previous.grade - _before.grade;
        public double DiffTier  => _previous.tier - _before.tier;


        public double[] PreviousExp    => _previous.exp;
        public double[] PreviousMaxExp => _previous.expMax;

        public double[] BeforeExp    => _before.exp;
        public double[] BeforeMaxExp => _before.expMax;

        public bool Valid => _previous.Valid && _before.Valid &&
                             _previous.uid == _before.uid && _previous.groupUid == _before.groupUid && _previous.infoUid == _before.infoUid;

        public ItemDiff() { }

        public ItemDiff(ItemValue previous, ItemValue before) {
            _previous = previous;
            _before   = before;
        }

        public void Set(ItemValue previous, ItemValue before) {
            _previous = previous;
            _before   = before;
        }
    }
}