using Cysharp.Threading.Tasks;
using Script.GameInfo.Dungeon;

namespace Script.GamePlay.Stage {
    public class ClientPlayerSpawnAction : ClientActionBase {
        private PlayerSpawnAction _playerSpawnAction;
        
        public ClientPlayerSpawnAction(ActionBase action) : base(action) {
            
        }

        public override UniTask Execute() {
            throw new System.NotImplementedException();
        }
    }
}