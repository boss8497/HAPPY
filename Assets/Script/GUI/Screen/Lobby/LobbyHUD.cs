using Cysharp.Threading.Tasks;
using Script.GameInfo.Attribute;
using Script.GUI.ScreenData.Interface;
using Script.Utility.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Script.GUI.Screen {
    public class LobbyHUD : Screen {
        [SerializeField, ScreenKey]
        private string runningStageScreen;
        public Button runningStageScreenBtn;
        
        [SerializeField, ScreenKey]
        private string logLikeScreen;
        
        
        protected override void AwakeInternal() {
            base.AwakeInternal();
            runningStageScreenBtn.ClickAddListener(() => {
                ScreenManager.OpenAsync(runningStageScreen);
            });
        }
        
        public override UniTask OpenInternal(IScreenOption data) {
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}