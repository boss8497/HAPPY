using System;
using R3;
using Script.GameData.Data;
using Script.GameData.Diff;
using Script.GameInfo.Enum;
using Script.GameInfo.Item;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using VContainer;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.GUI.ViewModel {
    public class DiffResultViewModel : ViewModelBase {
        #region Inject

        private IItemService _itemService;

        [Inject]
        public void Inject(
            IItemService itemService
        ) {
            _itemService = itemService;
        }

        #endregion

        #region Option

        [Flags]
        public enum DiffResultOption {
            None,
            CharacterLevelExp,
        }

        #endregion

        #region CharacterLevelExp

        [SerializeField, ShowIf("@option == DiffResultOption.CharacterLevelExp")]
        private TMP_Text characterName;

        [SerializeField, ShowIf("@option == DiffResultOption.CharacterLevelExp")]
        private TMP_Text characterLevel;

        [SerializeField, ShowIf("@option == DiffResultOption.CharacterLevelExp")]
        private TMP_Text characterExp;

        #endregion

        [SerializeField]
        private DiffResultOption option;

        public ReactiveProperty<DiffResult> DiffResult { get; set; } = new();


        public ReadOnlyReactiveProperty<ItemData>      ItemData      { get; set; }
        public ReadOnlyReactiveProperty<ItemInfo>      ItemInfo      { get; set; }
        public ReadOnlyReactiveProperty<CharacterInfo> CharacterInfo { get; set; }


        private DisposableBag _disposableBag;


        protected override void Initialize() {
            ItemData = DiffResult.Select(i => i == null ? Observable.Return<ItemData>(null) : _itemService.GetItem(i.ItemUid))
                                 .Switch()
                                 .ToReadOnlyReactiveProperty()
                                 .AddTo(ref _disposableBag);

            ItemInfo = DiffResult.Select(i => Observable.Return(i == null ? null : GameInfoManager.Instance.Get<ItemInfo>(i.ItemInfoUid)))
                                 .Switch()
                                 .ToReadOnlyReactiveProperty()
                                 .AddTo(ref _disposableBag);

            CharacterInfo = ItemInfo.Select(i => Observable.Return((i == null || i.type != ItemType.Character) ? null : GameInfoManager.Instance.Get<CharacterInfo>(i.characterInfoUid)))
                                    .Switch()
                                    .ToReadOnlyReactiveProperty()
                                    .AddTo(ref _disposableBag);


            // CharacterLevelExp
            DiffResult.CombineLatest(CharacterInfo, ItemData, (diff, characterInfo, itemData) => (diff, characterInfo, itemData))
                      .Subscribe(data => {
                          if (data.diff == null || data.characterInfo == null) {
                              if (characterName != null) {
                                  characterName.SetText(string.Empty);
                              }

                              if (characterLevel != null) {
                                  characterLevel.SetText(string.Empty);
                              }

                              if (characterExp != null) {
                                  characterExp.SetText(string.Empty);
                              }
                              
                              return;
                          }

                          if (characterName != null) {
                              characterName.SetText(data.itemData.ItemInfo.CurrentValue.Name);
                          }

                          if (characterLevel != null) {
                              characterLevel.SetText($"{data.itemData.Level.CurrentValue}");
                          }

                          if (characterExp != null) {
                              characterExp.SetText($"{data.itemData.Exp.CurrentValue}");
                          }
                      })
                      .AddTo(ref _disposableBag);
        }

        public override void Dispose() {
            throw new NotImplementedException();
        }
    }
}