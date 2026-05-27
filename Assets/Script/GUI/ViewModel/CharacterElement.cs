using System.Linq;
using R3;
using Script.GameData.Data;
using Script.GameInfo.Item;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Script.Utility.Runtime;
using Spine.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using VContainer;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.GUI.ViewModel {
    public class CharacterElement : SelectElement {
        private IItemService _itemService;

        [Inject]
        public void Inject(
            IItemService itemService
        ) {
            _itemService = itemService;
        }
        public Button selectButton;
        public Button disableButton;

        public GameObject disableObject;

        [SerializeField]
        private SkeletonGraphic skeletonGraphic;


        private ReactiveProperty<CharacterInfo> CharacterInfo { get; set; } = new();
        private ReactiveProperty<ItemInfo>      ItemInfo      { get; set; } = new();

        private ReadOnlyReactiveProperty<ItemData> Item    { get; set; }
        private ReadOnlyReactiveProperty<bool>     HasItem { get; set; }


        private AsyncOperationHandle<SkeletonDataAsset> _skeletonHandle;

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
                          .AddTo(ref  _disposableBag);

            CharacterInfo.SubscribeAwait(async (info, ct) => {
                             var isOn = info != null;
                             skeletonGraphic.SetActiveSafe(isOn);

                             if (_skeletonHandle.IsValid()) {
                                 Addressables.Release(_skeletonHandle);
                             }

                             if (isOn) {
                                 _skeletonHandle = Addressables.LoadAssetAsync<SkeletonDataAsset>(info.skeletonDataAsset);
                                 await _skeletonHandle.Task;
                                 skeletonGraphic.skeletonDataAsset = _skeletonHandle.Result;
                                 skeletonGraphic.Initialize(true);
                                 skeletonGraphic.StartAnimation("IDLE", true);
                             }
                         })
                         .AddTo(ref _disposableBag);

            HasItem.Subscribe(hasItem => {
                       selectButton.interactable  = hasItem;
                       disableButton.interactable = !hasItem;
                       
                       disableObject.SetActiveSafe(!hasItem);
                   })
                   .AddTo(ref _disposableBag);
        }

        public void SetReactive(CharacterInfo info) {
            _subScribeDisposableBag.Dispose();
            var characterItemInfo = GameInfoManager.Instance.GetCollection<ItemInfo>().FirstOrDefault(r => r.characterInfoUid == info.UID);

            CharacterInfo.OnNext(info);
            ItemInfo.OnNext(characterItemInfo);

            if ((characterItemInfo?.UID ?? 0) > 0) {
                _itemService.SubscribeItemInfoUidUpdate(info.UID)
                            .Subscribe(uid => { ItemInfo.ForceNotify(); })
                            .AddTo(ref _subScribeDisposableBag);
            }
        }

        private void OnDisable() {
            Release();
        }

        public void Release() {
            _disposableBag.Dispose();
            _subScribeDisposableBag.Dispose();
            if (_skeletonHandle.IsValid()) {
                Addressables.Release(_skeletonHandle);
            }
        }

        protected override void OnEnableInternal() {
            base.OnEnableInternal();
            selectButton.ClickAddListener(Select);
        }

        protected override void SetKey() {
            _key = 0;
        }

        public override void OnSelectInternal() {
        }
        public override void OnDeSelectInternal() {
        }
    }
}