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

        public override UniTask Execute() {
            var prefab = _stageManager.StagePooling.Pop(_characterInfo.prefab);
            prefab.transform.position = _enemySpawnAction.position;
            _stageManager.AddEnemy(prefab);

            return UniTask.CompletedTask;
        }

        public override UniTask Release() {
            return UniTask.CompletedTask;
        }
    }
}