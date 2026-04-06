using System;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.GamePlay.Stage {
    [System.Serializable]
    public class ClientEnemySpawnAction : ClientActionBase {
        private readonly EnemySpawnAction _enemySpawnAction;

        private IStageManager _stageManager;
        private CharacterInfo _characterInfo;

        public ClientEnemySpawnAction(ActionBase action) : base(action) {
            if (action is EnemySpawnAction enemySpawnAction) {
                _enemySpawnAction = enemySpawnAction;
            }
        }

        public override UniTask Initialize(IStageManager stageManager) {
            _stageManager  = stageManager;
            _characterInfo = GameInfoManager.Instance.Get<CharacterInfo>(_enemySpawnAction.uid);
            return UniTask.CompletedTask;
        }

        public override async UniTask Execute() {
            //GameConfig Load
            var prefabHandle = Addressables.LoadAssetAsync<GameObject>(_characterInfo.prefab);
            await prefabHandle.Task;

            if (prefabHandle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Load failed: {nameof(GameConfiguration)}");

            var prefab = _stageManager.Resolver.Instantiate(prefabHandle.Result);
            prefab.transform.position = _enemySpawnAction.position;
            _stageManager.AddEnemy(prefab);

            Addressables.Release(prefabHandle);
        }
    }
}