using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.DataBase.Enum;
using Script.GameData.Model;
using Script.GameInfo.Enum;
using Script.GameInfo.Item;
using Script.GameInfo.Table;

namespace Script.DataBase {
    public partial class GameDataBase {
        private readonly string itemModelTableName = $"{nameof(ItemModelTable)}.json";

        private ItemModelTable                                     _itemModelTable;
        private Dictionary<long, Dictionary<int, List<ItemModel>>> _itemModelByGroupUid;

        private async UniTask InitializeItemTable() {
            _itemModelTable = await LoadAsync<ItemModelTable>(itemModelTableName, DataType.Json);
            _itemModelByGroupUid = _itemModelTable.items
                                                  .GroupBy(g => g.groupUid)
                                                  .ToDictionary(e => e.Key, e =>
                                                                    e.GroupBy(g => g.infoUid).ToDictionary(g => g.Key, g => g.ToList()));
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
            if (_itemModelByGroupUid.TryGetValue(itemModel.groupUid, out var itemModels)) {
                if (itemModels.ContainsKey(itemModel.infoUid)) {
                    itemModels[itemModel.infoUid].Add(itemModel);
                }
                else {
                    itemModels[itemModel.infoUid] = new List<ItemModel>() { itemModel };
                }
            }
        }

        private ItemModel AddItemTableCount(
            long   groupUid,
            int    itemUid,
            double count = 1d,
            int    level = 1,
            int    grade = 0,
            int    tier  = 0
        ) {
            if (HasItem(groupUid, itemUid)) {
                var itemModel = _itemModelByGroupUid[groupUid][itemUid]?.FirstOrDefault(m => m.SameItem(level, grade, tier));
                if (itemModel != null) {
                    itemModel.count += count;
                    return itemModel;
                }
            }

            return CreateItemModel(groupUid, itemUid, count, level, grade, tier);
        }

        public ItemModel AddItem(
            long   groupUid,
            int    itemUid,
            double count = 1d,
            int    level = 1,
            int    grade = 0,
            int    tier  = 0
        ) {
            var itemInfo = GameInfoManager.Instance.Get<ItemInfo>(itemUid);
            if (itemInfo.flag.HasFlag(ItemFlag.Stack)) {
                AddItemTableCount(groupUid, itemUid, count, level, grade, tier);
            }

            return CreateItemModel(groupUid, itemUid, count, level, grade, tier);
        }

        public bool HasItem(long groupUid, int itemUid) {
            if (_itemModelByGroupUid.TryGetValue(groupUid, out var itemModels)) {
                return itemModels.ContainsKey(itemUid);
            }

            return false;
        }

        public async UniTask SaveItemTable() {
            await SaveAsync(itemModelTableName, _itemModelTable, DataType.Json);
        }
    }
}