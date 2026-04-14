using Cysharp.Threading.Tasks;
using Script.GamePlay.Stage;
using Script.Utility.Runtime;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class RunningFail : Screen {
        /// <summary>
        /// Inspector
        /// </summary>
        public Button restartBtn;

        /// <summary>
        /// Inject
        /// </summary>
        private IStageManager _stageManager;

        [Inject]
        public void InjectSelf(
            IStageManager stageManager
        ) {
            _stageManager = stageManager;
        }


        #region Override

        protected override void AwakeInternal() {
            base.AwakeInternal();
            restartBtn.ClickAddListener(Restart, false);
        }

        public override UniTask OpenInternal() {
            return UniTask.CompletedTask;
        }


        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }

        #endregion

        private void Restart() {
            _stageManager?.ReStart().Forget();
        }
    }
}