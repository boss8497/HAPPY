using Cysharp.Threading.Tasks;
using Script.GamePlay.Scene;
using Script.GamePlay.Stage;
using Script.GameSetting.Interface;
using Script.Utility.Runtime;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class RunningClear : Screen {
        /// <summary>
        /// Inspector
        /// </summary>
        public Button lobbyBtn;


        // Private
        private bool _enterLobby = false;

        /// <summary>
        /// Inject
        /// </summary>
        private IStageManager _stageManager;

        private IGameSetting _gameSetting;
        private ISceneLoader _sceneLoader;

        [Inject]
        public void InjectSelf(
            IStageManager stageManager,
            IGameSetting  gameSetting,
            ISceneLoader  sceneLoader
        ) {
            _stageManager = stageManager;
            _gameSetting  = gameSetting;
            _sceneLoader  = sceneLoader;
        }


        #region Override

        protected override void AwakeInternal() {
            base.AwakeInternal();
            lobbyBtn.ClickAddListener(EnterLobby, false);
        }

        public override UniTask OpenInternal() {
            return UniTask.CompletedTask;
        }


        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }

        #endregion

        private void EnterLobby() {
            if (_enterLobby) return;
            _enterLobby = true;
            EnterLobbyAsync().Forget();
        }

        private async UniTask EnterLobbyAsync() {
            await _sceneLoader.LoadScene(_gameSetting.LobbyScenePath);

            _enterLobby = false;
        }
    }
}