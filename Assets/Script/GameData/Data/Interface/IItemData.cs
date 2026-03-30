using R3;
using Script.GameData.Model;
using Script.GameInfo.Item;

namespace Script.GameData.Data.Interface {
    public interface IItemData : IData<ItemModel> {
        public ReadOnlyReactiveProperty<int>      ItemUid  { get; set; }
        public ReadOnlyReactiveProperty<int>      Level    { get; set; }
        public ReadOnlyReactiveProperty<int>      Grade    { get; set; }
        public ReadOnlyReactiveProperty<int>      Tier     { get; set; }
        public ReadOnlyReactiveProperty<ItemInfo> ItemInfo { get; set; }
    }
}