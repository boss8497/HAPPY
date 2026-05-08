using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;

namespace Script.GamePlay.Stage {
    public class ClientEnemySpawnArrayAction : ClientActionBase {
        private readonly EnemySpawnArrayAction _enemySpawnAction;

        private IStageManager _stageManager;
        private CharacterInfo _characterInfo;

        public ClientEnemySpawnArrayAction(ActionBase action) : base(action) {
            if (action is EnemySpawnArrayAction enemySpawnAction) {
                _enemySpawnAction = enemySpawnAction;
            }
        }
        public override UniTask Initialize(IStageManager stageManager) {
            _stageManager  = stageManager;
            _characterInfo = GameInfoManager.Instance.Get<CharacterInfo>(_enemySpawnAction.uid);
            return UniTask.CompletedTask;
        }
        public override UniTask Execute() {
            foreach (var position in _enemySpawnAction.positions) {
                var prefab = _stageManager.StagePooling.Pop(_characterInfo.prefab);
                prefab.transform.position = position;
            
                var result = _stageManager.AddEnemy(prefab);
                if (result == false) {
                    _stageManager.StagePooling.Push(prefab);
                }   
            }
            return UniTask.CompletedTask;
        }

        public override UniTask Release() {
            return UniTask.CompletedTask;
        }
    }
}