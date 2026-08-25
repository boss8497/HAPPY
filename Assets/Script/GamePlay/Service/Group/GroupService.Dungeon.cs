using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameData.Data;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Enum;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Service {
    public partial class GroupService {
        public async UniTask EnterDungeon(DungeonInfo dungeonInfo, Stage stage) {
            var result = await _client.Req_EnterDungeon(dungeonInfo, stage, 0);
            if (result) {
                _characterItem = null;
                _enterDungeon  = new(dungeonInfo, stage);

                await _sceneLoader.LoadScene(stage.scenePath);
                _audioManager.StopBGM();
                _audioManager.PlayBGM(stage.bgm.key).Forget();
            }
        }

        public async UniTask EnterDungeon(DungeonInfo dungeonInfo, Stage stage, ItemData character, bool loadScene = true) {
            if (character == null || _itemService.HasItem(character) == false) {
                await _screenManager.OpenErrorMessage(ErrorMessage.HasNotItemParam, CancellationToken.None);
                return;
            }

            var result = await _client.Req_EnterDungeon(dungeonInfo, stage, character.ItemUid.CurrentValue);
            if (result) {
                _characterItem = character;
                _enterDungeon  = new(dungeonInfo, stage);
                if (loadScene) {
                    await _sceneLoader.LoadScene(stage.scenePath);
                }

                _audioManager.StopBGM();
                _audioManager.PlayBGM(stage.bgm.key).Forget();
            }
        }

        public Tuple<DungeonInfo, Stage> GetEnterDungeon() {
            return _enterDungeon;
        }

        public ItemData GetCharacterItem() {
            return _characterItem;
        }

        public DungeonProgress GetDungeon(Category dungeonCategory) {
            var category = (int)dungeonCategory;
            return _groupData.Model.CurrentValue.dungeonProgresses?.FirstOrDefault(r => r.category == category);
        }

        public bool IsCleared(DungeonInfo dungeonInfo, Stage stage) {
            var dungeonProgress = GetDungeon((Category)dungeonInfo.category);
            if (dungeonProgress == null) {
                Debug.LogError($"Not found Category: {(Category)dungeonInfo.category}");
                return false;
            }

            if (dungeonProgress.dungeonUid != dungeonInfo.UID) {
                Debug.LogError($"Not found Dungeon {dungeonInfo.UID}");
                return false;
            }

            var stageIndex   = dungeonInfo.stages.FindIndex(r => r.guid.Value == stage.guid.Value);
            var clearedIndex = dungeonInfo.stages.FindIndex(r => r.guid.Value == dungeonProgress.stageGuid);

            return dungeonProgress.cleared ? stageIndex <= clearedIndex : stageIndex < clearedIndex;
        }

        public bool CanEnterStage(DungeonInfo dungeonInfo, Stage stage) {
            var dungeonProgress = GetDungeon((Category)dungeonInfo.category);
            if (dungeonProgress == null) {
                Debug.LogError($"Not found Category: {(Category)dungeonInfo.category}");
                return false;
            }

            if (dungeonProgress.dungeonUid != dungeonInfo.UID) {
                Debug.LogError($"Not found Dungeon {dungeonInfo.UID}");
                return false;
            }

            var stageIndex   = dungeonInfo.stages.FindIndex(r => r.guid.Value == stage.guid.Value);
            var clearedIndex = dungeonInfo.stages.FindIndex(r => r.guid.Value == dungeonProgress.stageGuid);

            return stageIndex <= clearedIndex;
        }

        public async UniTask<ItemSyncModel> ClearedDungeon(DungeonInfo dungeonInfo, Stage stage) {
            var itemSync = await _client.Req_ClearStage(dungeonInfo, stage, _characterItem?.ItemUid?.CurrentValue ?? 0);
            await _itemService.UpdateItems(itemSync.updateItems);
            return itemSync;
        }
    }
}