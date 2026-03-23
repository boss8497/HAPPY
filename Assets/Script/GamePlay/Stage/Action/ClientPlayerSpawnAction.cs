using System;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;
using Object = UnityEngine.Object;

namespace Script.GamePlay.Stage {
    public class ClientPlayerSpawnAction : ClientActionBase {
        private PlayerSpawnAction _playerSpawnAction;
        private IStageManager     _stageManager;
        private CharacterInfo     _characterInfo;

        public ClientPlayerSpawnAction(ActionBase action) : base(action) {
            if (action is PlayerSpawnAction playerSpawnAction) {
                _playerSpawnAction = playerSpawnAction;
            }
            else {
                throw new System.NotImplementedException("PlayerSpawnAction is not implemented");
            }
        }

        public override UniTask Initialize(IStageManager stageManager) {
            //일단 테스트 용으로 처음 캐릭터
            _stageManager = stageManager;
            _characterInfo = GameInfoManager.Instance.Get<CharacterInfo>(1);
            return UniTask.CompletedTask;
        }

        public override async UniTask Execute() {
            
            //GameConfig Load
            var prefabHandle = Addressables.LoadAssetAsync<GameObject>(_characterInfo.prefab);
            await prefabHandle.Task;
            
            if (prefabHandle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Load failed: {nameof(GameConfiguration)}");

            var prefab = _stageManager.Resolver.Instantiate(prefabHandle.Result);
            prefab.transform.position = _playerSpawnAction.position;
            _stageManager.AddCharacter(prefab);
            
            Addressables.Release(prefabHandle);
        }
    }
}