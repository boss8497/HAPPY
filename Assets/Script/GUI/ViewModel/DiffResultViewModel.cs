using System;
using R3;
using Script.GameData.Data;
using Script.GameData.Diff;
using Script.GameInfo.Enum;
using Script.GameInfo.Info.Enum;
using Script.GameInfo.Item;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Script.Localize.Text;
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
        private TMP_Text characterNameLevel;

        [SerializeField, ShowIf("@option == DiffResultOption.CharacterLevelExp")]
        private LocalizeText characterNameLevelLocalizeText;

        [SerializeField, ShowIf("@option == DiffResultOption.CharacterLevelExp")]
        private LocalizeText characterNameLevelUpLocalizeText;

        [SerializeField, ShowIf("@option == DiffResultOption.CharacterLevelExp")]
        private TMP_Text characterExp;

        [SerializeField, ShowIf("@option == DiffResultOption.CharacterLevelExp")]
        private LocalizeText characterExpLocalizeText;

        #endregion

        [SerializeField]
        private DiffResultOption option;

        public ReactiveProperty<ItemDiff> DiffResult { get; set; } = new();


        public ReadOnlyReactiveProperty<ItemData>      ItemData      { get; set; }
        public ReadOnlyReactiveProperty<ItemInfo>      ItemInfo      { get; set; }
        public ReadOnlyReactiveProperty<CharacterInfo> CharacterInfo { get; set; }


        private DisposableBag _disposableBag;


        protected override void Initialize() {
            ItemData = DiffResult.Select(i => {
                                     if (i == null) return Observable.Return<ItemData>(null);
                                     var item = _itemService.GetItem(i.ItemUid);
                                     return item == null ? Observable.Return<ItemData>(null) : item.AsObservable();
                                 })
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
                          if (data.diff == null || data.characterInfo == null || data.itemData == null) {
                              if (characterNameLevel != null) {
                                  characterNameLevel.SetText(string.Empty);
                              }

                              if (characterExp != null) {
                                  characterExp.SetText(string.Empty);
                              }

                              return;
                          }

                          if (characterNameLevel != null) {
                              if (data.diff.IsChangedLevel) {
                                  // TextFormat: Level, DiffLevel, Name
                                  characterNameLevel.SetText(characterNameLevelUpLocalizeText.GetText(data.itemData.Level.CurrentValue,
                                                                                                      data.diff.DiffLevel,
                                                                                                      data.itemData.ItemInfo.CurrentValue.Name));
                              }
                              else {
                                  // TextFormat: Level, Name
                                  characterNameLevel.SetText(characterNameLevelLocalizeText.GetText(data.itemData.Level.CurrentValue, data.itemData.ItemInfo.CurrentValue.Name));
                              }
                          }

                          if (characterExp != null) {
                              // TextFormat: Current Exp, +Exp, MaxExp
                              characterExp.SetText(characterExpLocalizeText.GetText(data.itemData.LevelExp.CurrentValue,
                                                                                    data.diff.GetDiffExp(LevelType.Level),
                                                                                    data.itemData.LevelExpMax.CurrentValue));
                          }
                      })
                      .AddTo(ref _disposableBag);
        }

        public override void Dispose() { }
    }
}