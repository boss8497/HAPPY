using Cysharp.Threading.Tasks;
using Script.GamePlay.Scene;
using Script.GameSetting.Interface;
using Script.LifetimeScope.Interface;
using Script.LifetimeScope.Locator;
using Script.Utility.Runtime;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class TitleHUD : Screen {
        // Inject
        private IGameSetting  _gameSetting;
        private ISceneLoader  _sceneLoader;
        private IScopeFactory _scopeFactory;

        [Inject]
        public void InjectSelf(
            IGameSetting gameSetting,
            ISceneLoader sceneLoader,
            IScopeFactory  scopeFactory
        ) {
            _gameSetting  = gameSetting;
            _sceneLoader  = sceneLoader;
            _scopeFactory = scopeFactory;
        }

        // Inspector
        public Button startBtn;

        
        // Private
        private bool _enterLobby = false;
        
        protected override void AwakeInternal() {
            base.AwakeInternal();
            startBtn.ClickAddListener(EnterLobby);
        }

        private void EnterLobby() {
            if (_enterLobby) return;
            _enterLobby = true;
            EnterLobbyAsync().Forget();
        }

        private async UniTask EnterLobbyAsync() {
            await CreateGroupScope();
            await _sceneLoader.LoadScene(_gameSetting.LobbyScenePath);

            _enterLobby = false;
        }
        
        private async UniTask CreateGroupScope() {
            await UniTask.WaitUntil(() => (_scopeFactory != null));
            _scopeFactory.CreateScope(ScopeType.Group);
        }


        public override UniTask OpenInternal() {
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}