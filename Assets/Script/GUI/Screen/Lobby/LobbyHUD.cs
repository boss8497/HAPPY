using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Attribute;
using Script.GameInfo.Info;
using Script.GamePlay.Audio.Interface;
using Script.GamePlay.Service.Interface;
using Script.GUI.ScreenData.Interface;
using Script.Utility.Runtime;
using SW.GUI;
using SW.GUI.Base;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class LobbyHUD : Screen {
        private IAudioManager _audioManager;
        private ITutorialService _tutorialService;

        [SerializeField, Tutorial]
        private int _lobbyTutorialUid; 

        [SerializeField, ScreenKey]
        private string runningStageScreen;

        public SW_GUI_BUTTON runningStageScreenBtn;

        [SerializeField, ScreenKey]
        private string logLikeScreen;

        [SerializeField, ScreenKey]
        private string optionScreen;

        public SW_GUI_BUTTON_BASE optionScreenBtn;

        [Inject]
        public void InjectSelf(
            IAudioManager    audioManager,
            ITutorialService tutorialService
        ) {
            _audioManager    = audioManager;
            _tutorialService = tutorialService;
        }


        protected override void AwakeInternal() {
            base.AwakeInternal();
            runningStageScreenBtn.AddClickListener(() => { ScreenManager.OpenAsync(runningStageScreen); });
            optionScreenBtn.AddClickAsyncListener(async () => { await ScreenManager.OpenAsync(optionScreen); });
        }

        public override UniTask OpenInternal(IScreenOption data, CancellationToken ct = default) {
            _tutorialService.StartTutorial(_lobbyTutorialUid);
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}