using R3;
using Script.GameData.Model;
using Script.GameInfo.Character;
using Script.GameInfo.Item;
using Script.GamePlay.Stat;

namespace Script.GameData.Data.Interface {
    public interface IItemData : IData<ItemModel> {
        ReadOnlyReactiveProperty<long>          GroupUid      { get; }
        ReadOnlyReactiveProperty<long>          ItemUid       { get; }
        ReadOnlyReactiveProperty<int>           ItemInfoUid   { get; }
        ReadOnlyReactiveProperty<int>           Level         { get; }
        ReadOnlyReactiveProperty<int>           Grade         { get; }
        ReadOnlyReactiveProperty<int>           Tier          { get; }
        ReadOnlyReactiveProperty<ItemInfo>      ItemInfo      { get; }
        ReadOnlyReactiveProperty<CharacterInfo> CharacterInfo { get; }
        ReadOnlyReactiveProperty<double[]>      Exp           { get; }
        ReadOnlyReactiveProperty<double[]>      ExpMax        { get; }
        ReadOnlyReactiveProperty<double>        Count         { get; }

        Status Status { get; }
        double Jump   { get; }
        double Health { get; }
        double Speed  { get; }
    }
}