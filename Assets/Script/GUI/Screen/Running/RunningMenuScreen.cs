using Cysharp.Threading.Tasks;
using Script.GamePlay.Scene;
using Script.GamePlay.Stage;
using Script.GameSetting.Interface;
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
        private IGameSetting  _gameSetting;

        [Inject]
        public void InjectSelf(
            IStageManager stageManager,
            IGameSetting gameSetting,
            ISceneLoader sceneLoader
        ) {
            _stageManager = stageManager;
            _gameSetting  = gameSetting;
            _sceneLoader  = sceneLoader;
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
            await _sceneLoader.LoadScene(_gameSetting.LobbyScenePath);

            _enterLobby = false;
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