using System;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Attribute;
using Script.GamePlay.Stage;
using VContainer;

namespace Script.GUI.Screen {
    public class RunningHUD : Screen {
        [ScreenKey]
        public string screenKey;
        
        private IStageManager _stageManager;

        [Inject]
        public void InjectSelf(
            IStageManager stageManager
        ) {
            _stageManager = stageManager;
        }
        
        
        public override UniTask OpenInternal() {
            return UniTask.CompletedTask;
        }
        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }

        public void Restart() {
            _stageManager.ReStart().Forget();
        }
    }
}