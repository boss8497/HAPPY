using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Enum;
using Script.GameInfo.Table;
using Script.GamePlay.Camera;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.GamePlay.Stage {
    [System.Serializable]
    public class ClientEnemySpawnAction : ClientActionBase {
        private readonly EnemySpawnAction _enemySpawnAction;

        private Queue<SpawnData> _spawnDataQueue;
        private IStageManager    _stageManager;
        private ICameraControls  _cameraControls;

        public ClientEnemySpawnAction(ActionBase action) : base(action) {
            if (action is EnemySpawnAction enemySpawnAction) {
                _enemySpawnAction = enemySpawnAction;
                _spawnDataQueue   = new(_enemySpawnAction.spawnData.OrderBy(o => o.position.x));
            }
        }

        public override void Initialize(IStageManager stageManager) {
            _stageManager   = stageManager;
            _cameraControls = _stageManager?.CameraControls;
        }

        public override async UniTask ExecuteAsync() {
            while (_spawnDataQueue.Count > 0) {
                if (_spawnDataQueue.Peek().position.x > _cameraControls.SpawnOffset) {
                    break;
                }
                
                var spawnData     = _spawnDataQueue.Dequeue();
                var characterInfo = GameInfoManager.Instance.Get<CharacterInfo>(spawnData.uid);
                var prefab        = _stageManager.StagePooling.Pop(characterInfo.prefab);
                prefab.transform.position = spawnData.position;

                var result = _stageManager.AddEnemy(prefab);
                if (result == false) {
                    _stageManager.StagePooling.Push(prefab);
                }
                
                await UniTask.Yield();
            }
        }

        public override void Release() {
            _stageManager   = null;
            _spawnDataQueue = null;
        }
    }
}