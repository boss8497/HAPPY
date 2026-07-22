using System.Linq;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Table;
using Script.GamePlay.Scene;
using Script.GamePlay.Service.Interface;
using Script.GamePlay.Stage;
using Script.GameSetting.Interface;
using Script.GUI.ScreenData.Interface;
using Script.Utility.Runtime;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class RunningMenuScreen : Screen {
        // Private
        private bool _enterLobby = false;

        // Inspector
        public Button restartBtn;
        public Button lobbyBtn;

        // Inject
        private IStageManager _stageManager;
        private ISceneLoader  _sceneLoader;
        private IGroupService _groupService;

        [Inject]
        public void InjectSelf(
            IStageManager stageManager,
            IGameSetting  gameSetting,
            ISceneLoader  sceneLoader,
            IGroupService groupService
        ) {
            _stageManager = stageManager;
            _sceneLoader  = sceneLoader;
            _groupService = groupService;
        }

        protected override void AwakeInternal() {
            base.AwakeInternal();
            restartBtn.ClickAddListener(Restart, false);
            lobbyBtn.ClickAddListener(EnterLobby);
        }

        private void EnterLobby() {
            if (_enterLobby) return;
            _enterLobby = true;
            EnterLobbyAsync().Forget();
        }

        private async UniTask EnterLobbyAsync() {
            var lobbyDungeonInfo = GameInfoManager.Instance.LobbyDungeonInfo;
            await _groupService.EnterDungeon(lobbyDungeonInfo, lobbyDungeonInfo.stages.FirstOrDefault());
            _enterLobby = false;
        }

        private void Restart() {
            _stageManager?.ReStart().Forget();
        }

        public override UniTask OpenInternal(IScreenOption data) {
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