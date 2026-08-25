using System;
using Cysharp.Threading.Tasks;
using Script.Client;
using Script.GameData.Data;
using Script.GameData.Data.Interface;
using Script.GameInfo.Dungeon;
using Script.GamePlay.Audio.Interface;
using Script.GamePlay.Scene;
using Script.GamePlay.Service.Interface;
using Script.GUI.Screen.Interface;
using VContainer.Unity;

namespace Script.GamePlay.Service {
    public partial class GroupService : IGroupService, IInitializable {
        private readonly IClient        _client;
        private readonly IItemService   _itemService;
        private readonly ISceneLoader   _sceneLoader;
        private readonly IScreenManager _screenManager;
        private readonly IAudioManager  _audioManager;

        private GroupData  _groupData;
        public  IGroupData GroupData => _groupData;
        public  long       GroupUid  => _groupData?.Model?.CurrentValue?.uid ?? 0;

        public bool Initialized { get; private set; }


        private Tuple<DungeonInfo, Stage> _enterDungeon;
        private ItemData                  _characterItem;

        public GroupService(
            IClient        client,
            IItemService   itemService,
            ISceneLoader   sceneLoader,
            IScreenManager screenManager,
            IAudioManager  audioManager
        ) {
            _client        = client;
            _itemService   = itemService;
            _sceneLoader   = sceneLoader;
            _screenManager = screenManager;
            _audioManager  = audioManager;
        }

        public void Initialize() {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync() {
            //첫 접속
            var model = await _client.Req_Group();
            _groupData = new GroupData(model);

            var items = await _client.Req_Inventory(GroupUid);
            await _itemService.InitializeAsync(items);

            Initialized = true;
        }

        public void Dispose() {
        }
    }
}