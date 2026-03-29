using System;
using R3;
using Script.GameData.Data.Interface;
using Script.GameData.Model;
using Script.GameInfo.Item;
using Script.GameInfo.Table;

namespace Script.GameData.Data {
    [System.Serializable]
    public class ItemData : IItemData, IDisposable {
        public ReactiveProperty<ItemModel> Model { get; private set; } = new();

        public ReadOnlyReactiveProperty<int>      ItemUid  { get; set; }
        public ReadOnlyReactiveProperty<int>      Level    { get; set; }
        public ReadOnlyReactiveProperty<int>      Grade    { get; set; }
        public ReadOnlyReactiveProperty<int>      Tier     { get; set; }
        public ReadOnlyReactiveProperty<ItemInfo> ItemInfo { get; set; }


        private DisposableBag _disposableBag;

        public ItemData(ItemModel model) {
            if (model == null) {
                throw new ArgumentException($"ItemData 생성할 때 ItemModel이 Null일 수 없습니다.");
            }


            ItemUid = Model.Select(i => i.infoUid)
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

            ItemInfo = ItemUid.Select(i => GameInfoManager.Instance.Get<ItemInfo>(i))
                              .DistinctUntilChanged()
                              .ToReadOnlyReactiveProperty()
                              .AddTo(ref _disposableBag);


            Model.OnNext(model);
        }

        public void Dispose() {
            _disposableBag.Dispose();
            Model?.Dispose();
        }
    }
}