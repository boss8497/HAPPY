using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Expression;
using Script.DataBase.Enum;
using Script.GameData.Model;
using Script.GameInfo;
using Script.GameInfo.Enum;
using Script.GameInfo.Info.Enum;
using Script.GameInfo.Item;
using Script.GameInfo.Table;

namespace Script.DataBase {
    public partial class GameDataBase {
        private readonly string itemModelTableName = $"{nameof(ItemModelTable)}.json";

        private ItemModelTable                                     _itemModelTable;
        private Dictionary<long, ItemModel>                        _itemModelByUid;
        private Dictionary<long, Dictionary<int, List<ItemModel>>> _itemModelByGroupUid;

        private async UniTask InitializeItemTable() {
            if (Exists(itemModelTableName)) {
                _itemModelTable = await LoadAsync<ItemModelTable>(itemModelTableName, DataType.Json);
                _itemModelByUid = _itemModelTable.items.ToDictionary(i => i.uid, i => i);
                _itemModelByGroupUid = _itemModelTable.items
                                                      .GroupBy(g => g.groupUid)
                                                      .ToDictionary(e => e.Key, e =>
                                                                        e.GroupBy(g => g.infoUid).ToDictionary(g => g.Key, g => g.ToList()))
                                    ?? new();
            }
            else {
                _itemModelTable      = new();
                _itemModelByUid      = new();
                _itemModelByGroupUid = new();
                await SaveItemTable();
            }
        }

        private ItemModel CreateItemModel(
            long   groupUid,
            int    itemUid,
            double count = 1d,
            int    level = 1,
            int    grade = 0,
            int    tier  = 0
        ) {
            var newUid = ++_itemModelTable.lastUid;
            var itemModel = new ItemModel() {
                count    = count,
                level    = level,
                grade    = grade,
                tier     = tier,
                infoUid  = itemUid,
                groupUid = groupUid,
                uid      = newUid,
            };
            AddItemTable(itemModel);
            return itemModel;
        }

        private void AddItemTable(ItemModel itemModel) {
            _itemModelTable.items.Add(itemModel);
            _itemModelByUid.Add(itemModel.uid, itemModel);
            if (_itemModelByGroupUid.TryGetValue(itemModel.groupUid, out var itemModels)) {
                if (itemModels.ContainsKey(itemModel.infoUid)) {
                    itemModels[itemModel.infoUid].Add(itemModel);
                }
                else {
                    itemModels[itemModel.infoUid] = new List<ItemModel>() { itemModel };
                }
            }
            else {
                var items = new List<ItemModel> { itemModel };
                _itemModelByGroupUid.Add(itemModel.groupUid, new Dictionary<int, List<ItemModel>>() { { itemModel.infoUid, items } });
            }
        }

        private ItemModel AddItemTableCount(
            long   groupUid,
            int    itemInfoUid,
            double count = 1d,
            int    level = 1,
            int    grade = 0,
            int    tier  = 0
        ) {
            if (HasItem(groupUid, itemInfoUid)) {
                var itemModel = _itemModelByGroupUid[groupUid][itemInfoUid]?.FirstOrDefault(m => m.SameItem(level, grade, tier));
                if (itemModel != null) {
                    itemModel.count += count;
                    return itemModel;
                }
            }

            return CreateItemModel(groupUid, itemInfoUid, count, level, grade, tier);
        }

        public ItemModel AddItem(
            long   groupUid,
            int    itemInfoUid,
            double count = 1d,
            int    level = 1,
            int    grade = 0,
            int    tier  = 0
        ) {
            var itemInfo = GameInfoManager.Instance.Get<ItemInfo>(itemInfoUid);
            if (itemInfo.flag.HasFlag(ItemFlag.Stack)) {
                AddItemTableCount(groupUid, itemInfoUid, count, level, grade, tier);
            }

            return CreateItemModel(groupUid, itemInfoUid, count, level, grade, tier);
        }

        public ItemModel AddItem(
            long       groupUid,
            ItemReward reward
        ) {
            var itemInfo = GameInfoManager.Instance.Get<ItemInfo>(reward.itemUid);
            if (itemInfo.flag.HasFlag(ItemFlag.Stack)) {
                AddItemTableCount(groupUid, reward.itemUid, reward.count, reward.level, reward.grade, reward.tier);
            }

            return CreateItemModel(groupUid, reward.itemUid, reward.count, reward.level, reward.grade, reward.tier);
        }

        public ItemModel[] AddRewards(
            long  groupUid,
            int[] rewardInfoUids
        ) {
            var items = new List<ItemModel>();

            foreach (var rewardInfoUid in rewardInfoUids) {
                var rewardInfo = GameInfoManager.Instance.Get<RewardInfo>(rewardInfoUid);
                foreach (var reward in rewardInfo.itemRewards) {
                    var itemInfo = GameInfoManager.Instance.Get<ItemInfo>(reward.itemUid);
                    items.Add(itemInfo.flag.HasFlag(ItemFlag.Stack)
                                  ? AddItemTableCount(groupUid, reward.itemUid, reward.count, reward.level, reward.grade, reward.tier)
                                  : CreateItemModel(groupUid, reward.itemUid, reward.count, reward.level, reward.grade, reward.tier));
                }
            }

            return items.ToArray();
        }

        public bool HasItem(long groupUid, int itemInfoUid) {
            if (_itemModelByGroupUid.TryGetValue(groupUid, out var itemModels)) {
                return itemModels.ContainsKey(itemInfoUid);
            }

            return false;
        }

        public UniTask<ItemModel> GetItem(long groupUid, long itemUid) {
            var item = _itemModelByUid.GetValueOrDefault(itemUid);
            return UniTask.FromResult(item == null || item.groupUid != groupUid ? null : item);
        }

        public async UniTask<ItemModel> CharacterExpUp(long groupUid, long itemUid, int expItemInfoUid, double count, LevelType levelType) {
            var characterItem = await GetItem(groupUid, itemUid);
            if (characterItem == null) {
                throw new Exception($"Not found Character Item: {itemUid}");
            }
            var characterItemInfo = characterItem.ItemInfo;
            if (characterItemInfo == null) {
                throw new Exception($"Not found Character ItemInfo: {itemUid}");
            }

            if (characterItemInfo.expUids.Length <= 0) {
                throw new Exception($"Character ItemInfo doesn't have expUids: {itemUid}");
            }

            var expInfoUid = characterItemInfo.expUids.FirstOrDefault(r => {
                var info = GameInfoManager.Instance.Get<ExpInfo>(r);
                return info.levelType == levelType && info.itemUid == expItemInfoUid;
            });

            if (expInfoUid <= 0) {
                throw new Exception($"Character ItemInfo doesn't have expUids: {itemUid}");
            }
            
            var expInfo        = GameInfoManager.Instance.Get<ExpInfo>(expInfoUid);
            var levelTypeIndex = (int)levelType;
            
            while (true) {
                using var _       = CreateValueContext(characterItem);

                var currentExp = characterItem.exp[levelTypeIndex];
                var limitExp   = expInfo.Calc();
            
                if (currentExp + count < limitExp) {
                    // 경험치만 추가    
                    characterItem = await AddExp(characterItem, count, levelType);
                    break;
                }
                
                // 레벨업 필요
                characterItem =  await LevelUpItem(characterItem, levelType);
                count         -= (limitExp - currentExp);
            }

            return characterItem;
        }

        private async UniTask<ItemModel> AddExp(ItemModel item, double exp, LevelType levelType) {
            item.exp[(int)levelType] += exp;
            await SaveItemTable();
            return item;
        }

        public async UniTask SaveItemTable() {
            await SaveAsync(itemModelTableName, _itemModelTable, DataType.Json);
        }

        public UniTask<ItemModel[]> GetInventory(long groupUid) {
            if (_itemModelByGroupUid.TryGetValue(groupUid, out var itemModels)) {
                return UniTask.FromResult(itemModels.SelectMany(i => i.Value).ToArray());
            }

            return UniTask.FromResult(Array.Empty<ItemModel>());
        }

        public async UniTask<ItemModel> LevelUpItem(ItemModel item, LevelType levelType) {
            switch (levelType) {
                case LevelType.Grade:
                    item.grade += 1;
                    break;
                case LevelType.Level:
                    item.level += 1;
                    break;
                case LevelType.Tier:
                    item.tier += 1;
                    break;
            }
            item.exp[(int)levelType] = 0;
            await SaveItemTable();
            return item;
        }

        public UniTask RemoveGroupItems(long groupUid) {
            _itemModelByGroupUid.Remove(groupUid);
            _itemModelTable.items = _itemModelTable.items.Where(r => r.groupUid != groupUid).ToList();
            _itemModelByUid.Where(r => r.Value.groupUid == groupUid)
                          .Select(r => r.Key)
                          .ToList()
                          .ForEach(k => _itemModelByUid.Remove(k));
            
            return UniTask.CompletedTask;
        }
        
        
        
        //
        
        private ValueContext CreateValueContext(
            ItemModel item,
            int       levelOffset = 0,
            int       gradeOffset = 0,
            int       tierOffset  = 0
        ) {
            return new(
                new ValueProvider()
                    .Add("level", (item?.level ?? 1) + levelOffset)
                    .Add("grade", (item?.grade ?? 0) + gradeOffset)
                    .Add("tier", (item?.tier ?? 0) + tierOffset)
            );
        }
    }
}