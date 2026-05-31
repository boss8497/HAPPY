using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Character;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Enum;
using Script.GameInfo.Item;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Script.GUI.ScreenData.Interface;
using Script.GUI.ViewModel;
using Script.Localize.Text;
using Script.Utility.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.GUI.Screen {
    public class RunningStageScreen : Selector {
        // Reactive
        private IGroupService   _groupService;
        private IObjectResolver _objectResolver;
        private IItemService    _itemService;

        [Inject]
        public void InjectSelf(
            IGroupService   groupService,
            IObjectResolver objectResolver,
            IItemService    itemService
        ) {
            _groupService   = groupService;
            _objectResolver = objectResolver;
            _itemService    = itemService;
        }


        // Private Field
        private DungeonProgress _dungeonProgress;
        private DungeonInfo     _dungeonInfo;
        private Stage           _stage;


        // Inspector
        // Stage
        public AssetReferenceT<GameObject> stageElement;
        public RectTransform               stageContentRoot;
        public ErrorMessage                stageErrorMessage;


        // Character
        public AssetReferenceT<GameObject> characterElement;
        public RectTransform               characterContentRoot;
        public ErrorMessage                characterErrorMessage;

        // CharacterInfo
        public TMP_Text     nameText;
        public TMP_Text     healthText;
        public LocalizeText healthTextFormat;
        public TMP_Text     speedText;
        public LocalizeText speedTextFormat;
        public TMP_Text     jumpText;
        public LocalizeText jumpTextFormat;


        private List<CharacterElement> _characterElements;

        public override async UniTask OpenInternal(IScreenOption data) {
            await UniTask.WaitUntil(() => _groupService.Initialized);
            _dungeonProgress = _groupService.GetDungeon(Category.Running);
            _dungeonInfo     = GameInfoManager.Instance.Get<DungeonInfo>(_dungeonProgress.dungeonUid);
            _stage           = _dungeonInfo.stages.FirstOrDefault(r => r.guid.Value == _dungeonProgress.stageGuid);

            foreach (var stage in _dungeonInfo.stages) {
                var obj                = PoolPop(this.stageElement.AssetGUID, stageContentRoot);
                var stageElementScript = obj.GetComponent<StageElement>();
                if (stageElementScript != null) {
                    stageElementScript.InitializeReactive();
                    stageElementScript.SetReactive(stage, _dungeonInfo);

                    if (stageElementScript.startBtn != null) {
                        stageElementScript.startBtn.ClickAddListener(() => {
                            if (stageElementScript.Stage?.CurrentValue == null || stageElementScript.DungeonInfo?.CurrentValue == null) return;
                            var selectCharacter = _characterElements.FirstOrDefault(r => r.Selected);
                            _groupService.EnterDungeon(stageElementScript.DungeonInfo.CurrentValue, stageElementScript.Stage.CurrentValue, selectCharacter?.Item?.CurrentValue).Forget();
                        });
                    }

                    if (stageElementScript.errorBtn != null) {
                        stageElementScript.errorBtn.ClickAddListener(() => { ScreenManager.OpenErrorMessage(stageErrorMessage, CancellationToken.None).Forget(); });
                    }
                }
            }

            if (_characterElements != null) {
                ListPool.Return(_characterElements);
            }

            _characterElements = ListPool.Get<CharacterElement>();
            foreach (var character in GameInfoManager.Instance.GetCollection<CharacterInfo>().Where(r => r.type == CharacterType.Character)) {
                var obj = PoolPop(this.characterElement.AssetGUID, characterContentRoot);
                if (obj == null) continue;
                var characterElementScript = obj.GetComponent<CharacterElement>();
                if (characterElementScript != null) {
                    characterElementScript.InitializeReactive();
                    await characterElementScript.SetReactive(character);
                    characterElementScript.selectButton.ClickAddListener(() => { SelectCharacter(character); });
                    _characterElements.Add(characterElementScript);
                }
            }

            var firstSelect = _characterElements.FirstOrDefault();
            if (firstSelect != null) {
                firstSelect.Selector = this;
                firstSelect.Select();
                SelectCharacter(firstSelect?.CharacterInfo?.CurrentValue);
            }

            await UniTask.Yield();
        }

        private void SelectCharacter(CharacterInfo characterInfo) {
            if (characterInfo == null) return;
            
            if (nameText != null) {
                nameText.SetText(characterInfo.Name ?? "");
            }

            var characterItemInfo = GameInfoManager.Instance.GetCollection<ItemInfo>().FirstOrDefault(r => r.characterInfoUid == characterInfo.UID);
            if (characterItemInfo == null) {
                if (healthText != null) {
                    healthText.SetText(healthTextFormat.GetText(0));
                }

                if (speedText != null) {
                    speedText.SetText(speedTextFormat.GetText(0));
                }

                if (jumpText != null) {
                    jumpText.SetText(jumpTextFormat.GetText(0));
                }
            }
            else {
                var characterItem = _itemService.GetItem(characterItemInfo.UID);
                if (healthText != null) {
                    healthText.SetText(healthTextFormat.GetText(characterItem.Value.Health));
                }

                if (speedText != null) {
                    speedText.SetText(speedTextFormat.GetText(characterItem.Value.Speed));
                }

                if (jumpText != null) {
                    jumpText.SetText(jumpTextFormat.GetText(characterItem.Value.Jump));
                }
            }
        }

        public override UniTask CloseInternal() {
            if (_characterElements != null) {
                ListPool.Return(_characterElements);
            }

            ReleaseSelector();
            return UniTask.CompletedTask;
        }
    }
}