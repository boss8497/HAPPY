using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Script.Client;
using Script.GameData.Data;
using Script.GameInfo.Info.Enum;
using Script.GamePlay.Service.Interface;
using VContainer.Unity;

namespace Script.GamePlay.Service {
    /// <summary>
    /// 같은 Item의 Uid라도 Grade, Tier에 따라 여러개가 존재할 수 있음
    /// </summary>
    public class ItemService : IItemService, IInitializable {
        private readonly IClient       _client;
        private readonly IGroupService _groupService;

        private ItemData[] _items;
        // 기획 데이터의 Uid
        private Dictionary<int, List<ItemData>> _itemsByInfoUid;
        // DataBase의 Uid
        private Dictionary<long, ItemData> _itemsByUid;

        public bool Initialized { get; private set; }


        public ItemService(
            IClient       client,
            IGroupService groupService
        ) {
            _client       = client;
            _groupService = groupService;
        }


        public void Initialize() {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync() {
            await UniTask.WaitUntil(() => _groupService.Initialized);
            var groupUid = _groupService.GroupUid;
            if (groupUid == 0)
                throw new System.Exception("GroupUid is 0");

            var items = await _client.Req_Inventory(groupUid);
            
            _items = items.Select(i => new ItemData(i)).ToArray();
            _itemsByInfoUid = _items.GroupBy(i => i.ItemInfoUid.CurrentValue)
                                    .ToDictionary(i => i.Key, i => i.ToList());
            _itemsByUid = _items.ToDictionary(i => i.ItemUid.CurrentValue, i => i);

            Initialized = true;
        }
        
        [CanBeNull]
        public ItemData GetItem(int infoUid) {
            if(_itemsByInfoUid.TryGetValue(infoUid, out var items)) {
                return items.FirstOrDefault();
            }
            return null;
        }

        public async UniTask ItemLevelUp(ItemData item, LevelType type) {
            var model = await _client.Req_ItemLevelUp(item.Model.CurrentValue, type);
            item.Update(model);
        }
    }
}