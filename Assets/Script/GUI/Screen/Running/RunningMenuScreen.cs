using Cysharp.Threading.Tasks;
using Script.GamePlay.Stage;
using Script.Utility.Runtime;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class RunningMenuScreen : Screen {
        
        public Button restartBtn;
        
        private IStageManager _stageManager;

        [Inject]
        public void InjectSelf(
            IStageManager stageManager
        ) {
            _stageManager = stageManager;
        }

        protected override void AwakeInternal() {
            base.AwakeInternal();
            restartBtn.ClickAddListener(Restart, false);
        }

        private void Restart() {
            _stageManager?.ReStart().Forget();
        }


        public override UniTask OpenInternal() {
            _stageManager.Pause();
            _stageManager.AddState(StageState.SystemControl);
            return UniTask.CompletedTask;
        }
        
        public override UniTask CloseInternal() {
            _stageManager.Resume();
            _stageManager.RemoveState(StageState.SystemControl);
            return UniTask.CompletedTask;
        }
    }
}