using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;

namespace Script.GamePlay.Stage {
    public abstract class ClientActionBase {
        public ClientActionBase(ActionBase action) {
        }
        public abstract UniTask Initialize(IStageManager  stageManager);
        public abstract UniTask Execute();
    }
}