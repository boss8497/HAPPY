using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Attribute;
using Script.GamePlay.Audio.Interface;
using Script.GUI.ScreenData.Interface;
using Script.Utility.Runtime;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class LobbyHUD : Screen {
        private IAudioManager _audioManager;
        
        [SerializeField, ScreenKey]
        private string runningStageScreen;
        public Button runningStageScreenBtn;
        
        [SerializeField, ScreenKey]
        private string logLikeScreen;
        
        [SerializeField, ScreenKey]
        private string optionScreen;
        public Button optionScreenBtn;
        
        [Inject]
        public void InjectSelf(
            IAudioManager  audioManager
        ) {
            _audioManager = audioManager;
        }
        
        
        protected override void AwakeInternal() {
            base.AwakeInternal();
            runningStageScreenBtn.ClickAddListener(() => {
                ScreenManager.OpenAsync(runningStageScreen);
            });
            
            optionScreenBtn.ClickAddListener(() => {
                ScreenManager.OpenAsync(optionScreen);
            });
        }
        
        public override UniTask OpenInternal(IScreenOption data, CancellationToken ct = default) {
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}