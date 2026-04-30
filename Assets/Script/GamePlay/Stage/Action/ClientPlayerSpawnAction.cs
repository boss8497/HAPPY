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
    //TODO: 테스트 코드가 있음
    [System.Serializable]
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
            // 일단 테스트 용으로 처음 캐릭터
            // 이 후에는 선택된 캐릭터를 플레이할 수 있도록..
            _stageManager = stageManager;
            _characterInfo = GameInfoManager.Instance.Get<CharacterInfo>(1);
            return UniTask.CompletedTask;
        }

        public override UniTask Execute() {
            // 여기서 Pop한 Pool을 StageManager에서 Push를 해줌
            // Pop을 호출 했으면 StageManager에서 Push 로직 필요
            var prefab = _stageManager.StagePooling.Pop(_characterInfo.prefab);
            prefab.transform.position = _playerSpawnAction.position;
            
            if (_stageManager.AddCharacter(prefab) == false) {
                _stageManager.StagePooling.Push(prefab);
            }
            return UniTask.CompletedTask;
        }

        public override UniTask Release() {
            return UniTask.CompletedTask;
        }
    }
}