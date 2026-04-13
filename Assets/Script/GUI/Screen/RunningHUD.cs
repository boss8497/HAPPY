using System;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Attribute;
using Script.GamePlay.Stage;
using Script.Utility.Runtime;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class RunningHUD : Screen {
        [ScreenKey]
        public string screenKey;

        public Button optionBtn;

        protected override void AwakeInternal() {
            base.AwakeInternal();
            
            optionBtn.ClickAddListener(OpenOption);
        }

        public override UniTask OpenInternal() {
            return UniTask.CompletedTask;
        }
        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }

        private void OpenOption() {
            ScreenManager.OpenAsync(screenKey).Forget();
        }
    }
}