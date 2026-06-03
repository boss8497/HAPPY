using System;
using Expression;
using R3;
using Script.GameData.Data.Interface;
using Script.GameData.Model;
using Script.GameInfo.Item;
using Script.GameInfo.Character;
using Script.GameInfo.Info.Stat;
using Script.GameInfo.Table;
using Script.GamePlay.Stat;
using Script.Utility.Runtime;

namespace Script.GameData.Data {
    [System.Serializable]
    public class ItemData : IItemData, IDisposable {
        public ReactiveProperty<ItemModel> Model { get; private set; } = new();

        public ReadOnlyReactiveProperty<long>          ItemUid       { get; private set; }
        public ReadOnlyReactiveProperty<int>           ItemInfoUid   { get; private set; }
        public ReadOnlyReactiveProperty<int>           Level         { get; private set; }
        public ReadOnlyReactiveProperty<int>           Grade         { get; private set; }
        public ReadOnlyReactiveProperty<int>           Tier          { get; private set; }
        public ReadOnlyReactiveProperty<double[]>      Exp           { get; private set; }
        public ReadOnlyReactiveProperty<ItemInfo>      ItemInfo      { get; private set; }
        public ReadOnlyReactiveProperty<CharacterInfo> CharacterInfo { get; private set; }


        private Status _status;
        public  Status Status => _status;
        public  double Jump   => Status.Jump;
        public  double Health => Status.Hp;
        public  double Speed  => Status.Spd;

        private DisposableBag _disposableBag;

        public ItemData(ItemModel model) {
            if (model == null) {
                throw new ArgumentException($"ItemData 생성할 때 ItemModel이 Null일 수 없습니다.");
            }

            _status = ClassPool.Get<Status>();

            ItemUid = Model.Select(i => i.uid)
                           .DistinctUntilChanged()
                           .ToReadOnlyReactiveProperty()
                           .AddTo(ref _disposableBag);

            ItemInfoUid = Model.Select(i => i.infoUid)
                               .DistinctUntilChanged()
                               .ToReadOnlyReactiveProperty()
                               .AddTo(ref _disposableBag);

            Level = Model.Select(i => i.level)
                         .DistinctUntilChanged()
                         .ToReadOnlyReactiveProperty()
                         .AddTo(ref _disposableBag);

            Grade = Model.Select(i => i.grade)
                         .DistinctUntilChanged()
                         .ToReadOnlyReactiveProperty()
                         .AddTo(ref _disposableBag);

            Tier = Model.Select(i => i.tier)
                        .DistinctUntilChanged()
                        .ToReadOnlyReactiveProperty()
                        .AddTo(ref _disposableBag);

            Exp = Model.Select(i => i.exp)
                       .DistinctUntilChanged()
                       .ToReadOnlyReactiveProperty()
                       .AddTo(ref _disposableBag);

            ItemInfo = ItemInfoUid.Select(i => GameInfoManager.Instance.Get<ItemInfo>(i))
                                  .DistinctUntilChanged()
                                  .ToReadOnlyReactiveProperty()
                                  .AddTo(ref _disposableBag);

            CharacterInfo = ItemInfo.Select(i => {
                                        var characterUid = i?.characterInfoUid ?? 0;
                                        if (characterUid <= 0) return null;
                                        return GameInfoManager.Instance.Get<CharacterInfo>(characterUid);
                                    })
                                    .DistinctUntilChanged()
                                    .ToReadOnlyReactiveProperty()
                                    .AddTo(ref _disposableBag);

            CharacterInfo.CombineLatest(Level, Grade, Tier, (characterInfo, level, grade, tier) => (characterInfo, level, grade, tier))
                         .Subscribe(data => {
                             if (data.characterInfo == null) return;
                             _status.Clear();
                             using var _ = CreateValueContext();
                             foreach (var statusUid in data.characterInfo.statusUids) {
                                 _status.Add(GameInfoManager.Instance.Get<StatusInfo>(statusUid));
                             }
                         })
                         .AddTo(ref _disposableBag);

            Update(model);
        }


        public void Update(ItemModel itemModel) {
            Model.OnNext(itemModel);
        }

        private ValueContext CreateValueContext(
            int levelOffset = 0,
            int gradeOffset = 0,
            int tierOffset  = 0
        ) {
            return new(
                new ValueProvider()
                    .Add("level", (Level?.CurrentValue ?? 1) + levelOffset)
                    .Add("grade", (Grade?.CurrentValue ?? 0) + gradeOffset)
                    .Add("tier", (Tier?.CurrentValue ?? 0) + tierOffset)
            );
        }

        public void Dispose() {
            _disposableBag.Dispose();
            Model?.Dispose();

            _status.Clear();
            ClassPool.Release(_status);
            _status = null;
        }
    }
}