using System.Linq;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Attribute;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Script.LifetimeScope.Interface;
using Script.LifetimeScope.Locator;
using Script.Utility.Runtime;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class TitleHUD : Screen {
        // Inject
        private IScopeFactory _scopeFactory;

        [Inject]
        public void InjectSelf(
            IScopeFactory scopeFactory
        ) {
            _scopeFactory = scopeFactory;
        }

        // Inspector
        [SerializeField, Dungeon]
        private int lobbyDungeonUid;

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
            var groupLifeTimeScope = await CreateGroupScope();
            var lobbyDungeonInfo   = GameInfoManager.Instance.Get<DungeonInfo>(lobbyDungeonUid);
            var group              = groupLifeTimeScope.Container.Resolve<IGroupService>();
            await group.EnterDungeon(lobbyDungeonInfo, lobbyDungeonInfo.stages.FirstOrDefault());

            _enterLobby = false;
        }

        private async UniTask<VContainer.Unity.LifetimeScope> CreateGroupScope() {
            await UniTask.WaitUntil(() => (_scopeFactory != null));
            return _scopeFactory.CreateScope(ScopeType.Group);
        }


        public override UniTask OpenInternal() {
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}