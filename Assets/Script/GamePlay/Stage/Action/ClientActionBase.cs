using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Enum;

namespace Script.GamePlay.Stage {
    public abstract class ClientActionBase {
        protected         ActionBase  _actionBase;
        public EventTiming Timing => _actionBase.timing;

        public ClientActionBase(ActionBase action) {
            _actionBase = action;
        }
        public abstract void    Initialize(IStageManager stageManager);
        public abstract UniTask ExecuteAsync();
        public abstract void Release();
    }
}