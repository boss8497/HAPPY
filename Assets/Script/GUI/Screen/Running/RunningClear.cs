using Cysharp.Threading.Tasks;
using Script.GameData.Diff;
using Script.GameInfo.Table;
using Script.GamePlay.Scene;
using Script.GamePlay.Stage;
using Script.GameSetting.Interface;
using Script.GUI.ScreenData.Interface;
using Script.GUI.ViewModel;
using Script.Utility.Runtime;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class RunningClear : Screen {
        /// <summary>
        /// Inspector
        /// </summary>
        public Button lobbyBtn;
        
        [SerializeField]
        private DiffResultViewModel diffResultViewModel;


        // Private
        private bool _enterLobby = false;

        /// <summary>
        /// Inject
        /// </summary>
        private IStageManager _stageManager;
        private ISceneLoader _sceneLoader;

        [Inject]
        public void InjectSelf(
            IStageManager stageManager,
            IGameSetting  gameSetting,
            ISceneLoader  sceneLoader
        ) {
            _stageManager = stageManager;
            _sceneLoader  = sceneLoader;
        }


        #region Override

        protected override void AwakeInternal() {
            base.AwakeInternal();
            lobbyBtn.ClickAddListener(EnterLobby, false);
        }

        public override async UniTask OpenInternal(IScreenOption data) {
            if (diffResultViewModel != null && data is ItemDiff diffResult) {
                await UniTask.WaitUntil(() => diffResultViewModel.IsInitialize);
                diffResultViewModel.DiffResult.OnNext(diffResult);
            }
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
            await _sceneLoader.LoadScene(GameInfoManager.Instance.Config.lobbyScenePath);

            _enterLobby = false;
        }
    }
}