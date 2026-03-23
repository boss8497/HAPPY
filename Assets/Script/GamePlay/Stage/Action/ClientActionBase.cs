using Cysharp.Threading.Tasks;
using Script.GameInfo.Dungeon;

namespace Script.GamePlay.Stage {
    public abstract class ClientActionBase {
        public ClientActionBase(ActionBase action) {
        }
        public abstract UniTask Execute();
    }
}