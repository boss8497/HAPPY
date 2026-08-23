using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using Script.Addressable;
using Script.GameData.Data;
using Script.GameInfo.Item;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Script.Utility.Runtime;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.GUI.ViewModel {
    public class CharacterElement : SelectElement {
        private IItemService  _itemService;
        private IAddressableService  _addressableService;

        [Inject]
        public void Inject(
            IItemService itemService,
            IAddressableService addressableService
        ) {
            _itemService = itemService;
            _addressableService = addressableService;
        }

        public Button selectButton;
        public Button disableButton;

        public GameObject disableObject;

        [SerializeField]
        private SkeletonGraphic skeletonGraphic;


        public ReactiveProperty<CharacterInfo> CharacterInfo { get; private set; } = new();
        public ReactiveProperty<ItemInfo>      ItemInfo      { get; private set; } = new();

        public ReadOnlyReactiveProperty<ItemData> Item    { get; set; }
        public ReadOnlyReactiveProperty<bool>     HasItem { get; set; }


        private AddressableCacheHandle<SkeletonDataAsset> _skeletonHandle;

        private DisposableBag _disposableBag;
        private DisposableBag _subScribeDisposableBag;


        public void InitializeReactive() {
            _disposableBag = new();

            Item = ItemInfo.Select(i => {
                               if (i == null) return Observable.Return<ItemData>(null);
                               var item = _itemService.GetItem(i.UID);
                               return item == null ? Observable.Return<ItemData>(null) : item.AsObservable();
                           })
                           .Switch()
                           .ToReadOnlyReactiveProperty()
                           .AddTo(ref _disposableBag);

            HasItem = Item.Select(i => i != null)
                          .DistinctUntilChanged()
                          .ToReadOnlyReactiveProperty()
                          .AddTo(ref _disposableBag);

            HasItem.Subscribe(hasItem => {
                       selectButton.interactable  = hasItem;
                       disableButton.interactable = !hasItem;

                       disableObject.SetActiveSafe(!hasItem);
                   })
                   .AddTo(ref _disposableBag);
        }

        public async UniTask SetReactive(CharacterInfo info) {
            _subScribeDisposableBag.Dispose();
            var characterItemInfo = GameInfoManager.Instance.GetCollection<ItemInfo>().FirstOrDefault(r => r.characterInfoUid == info.UID);

            CharacterInfo.OnNext(info);
            ItemInfo.OnNext(characterItemInfo);

            if ((characterItemInfo?.UID ?? 0) > 0) {
                _itemService.SubscribeItemInfoUidUpdate(info.UID)
                            .Subscribe(uid => { ItemInfo.ForceNotify(); })
                            .AddTo(ref _subScribeDisposableBag);
            }

            await UpdateSkeleton(info);
        }

        private async UniTask UpdateSkeleton(CharacterInfo info) {
            var isOn = info != null;
            skeletonGraphic.SetActiveSafe(isOn);

            _skeletonHandle?.Dispose();
            _skeletonHandle = null;

            if (isOn) {
                _skeletonHandle = await _addressableService.LoadAsync<SkeletonDataAsset>(info.skeletonDataAsset);
                skeletonGraphic.skeletonDataAsset = _skeletonHandle.Value;

                skeletonGraphic.Initialize(true);
                skeletonGraphic.StartAnimation("IDLE", true);
            }
        }

        private void OnDisable() {
            Release();
        }

        public void Release() {
            _disposableBag.Dispose();
            _subScribeDisposableBag.Dispose();
            _skeletonHandle?.Dispose();
            _skeletonHandle = null;

            CharacterInfo.OnNext(null);
            ItemInfo.OnNext(null);
        }

        protected override void OnEnableInternal() {
            base.OnEnableInternal();
            selectButton.ClickAddListener(Select);
        }

        protected override void SetKey() {
            _key = 0;
        }

        public override void OnSelectInternal()   { }
        public override void OnDeSelectInternal() { }
    }
}