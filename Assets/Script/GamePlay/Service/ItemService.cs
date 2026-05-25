using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using R3;
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

        private List<ReactiveProperty<ItemData>> _items;

        // 기획 데이터의 Uid
        private Dictionary<int, List<ReactiveProperty<ItemData>>> _itemsByInfoUid;

        // DataBase의 Uid
        private Dictionary<long, ReactiveProperty<ItemData>> _itemsByUid;

        private Dictionary<int, ReactiveProperty<int>> _itemInfoUidSubscriber = new();

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

            _items = items.Select(i => {
                              var reactive = new ReactiveProperty<ItemData>();
                              reactive.OnNext(new ItemData(i));
                              return reactive;
                          })
                          .ToList();
            _itemsByInfoUid = _items.GroupBy(i => i.Value.ItemInfoUid.CurrentValue)
                                    .ToDictionary(i => i.Key, i => i.ToList());
            _itemsByUid = _items.ToDictionary(i => i.Value.ItemUid.CurrentValue, i => i);

            Initialized = true;
        }

        [CanBeNull]
        public ReactiveProperty<int> SubscribeItemInfoUid(int infoUid) {
            if (infoUid <= 0) return null;
            
            if(_itemInfoUidSubscriber.TryGetValue(infoUid, out var subscriber)) {
                return subscriber;
            }
            var reactive = new ReactiveProperty<int>(infoUid);
            _itemInfoUidSubscriber.Add(infoUid, reactive);
            return reactive;
        }

        [CanBeNull]
        public ReactiveProperty<ItemData> GetItem(int infoUid) {
            if (_itemsByInfoUid.TryGetValue(infoUid, out var items)) {
                return items.FirstOrDefault();
            }

            return null;
        }

        [CanBeNull]
        public List<ReactiveProperty<ItemData>> GetItems(int infoUid) {
            return _itemsByInfoUid.GetValueOrDefault(infoUid);
        }

        [CanBeNull]
        public ReactiveProperty<ItemData> GetItem(long itemUid) {
            return _itemsByUid.GetValueOrDefault(itemUid);
        }

        public void AddItem(ItemData item) {
            // 어짜피 1개의 래퍼를 가지고 있기 때문에 같으면 하나만 업데이트 해주면 다 업데이트
            // Update
            if (_itemsByUid.ContainsKey(item.ItemUid.CurrentValue)) {
                _itemsByUid[item.ItemUid.CurrentValue].OnNext(item);
            }
            // Add
            else {
                var reactive = new ReactiveProperty<ItemData>(item);
                _items.Add(reactive);
                _itemsByUid.Add(item.ItemUid.CurrentValue, reactive);
                if(_itemsByInfoUid.TryGetValue(item.ItemInfoUid.CurrentValue, out var items)) {
                    items.Add(reactive);
                }
                else {
                    _itemsByInfoUid.Add(item.ItemInfoUid.CurrentValue, new List<ReactiveProperty<ItemData>> { reactive });
                }
                reactive.ForceNotify();
            }

            UpdateItemInfoUid(item.ItemInfoUid.CurrentValue);
        }

        private void UpdateItemInfoUid(int infoUid) {
            if(_itemInfoUidSubscriber.TryGetValue(infoUid, out var subscriber)) {
                subscriber.ForceNotify();
            }
        }

        public async UniTask ItemLevelUp(ItemData item, LevelType type) {
            var itemData = GetItem(item.ItemUid.CurrentValue);
            if (itemData == null) {
                throw new System.Exception("ItemData is null");
            }

            var model = await _client.Req_ItemLevelUp(item.Model.CurrentValue, type);
            item.Update(model);
            itemData.ForceNotify();
            UpdateItemInfoUid(item.ItemInfoUid.CurrentValue);
        }
    }
}