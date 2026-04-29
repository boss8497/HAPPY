using Script.GamePlay.Camera;
using Script.GamePlay.ECS.World;
using Script.GamePlay.Pool;
using Script.GamePlay.Service.Interface;
using Script.GameTimer;
using Script.GUI.Screen.Interface;
using Unity.Cinemachine;
using VContainer;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private readonly CinemachineTargetGroup _targetGroup;
        private readonly IStageEntityWorld      _entityWorld;
        private readonly IScreenManager         _screenManager;
        private readonly IGameTimer             _gameTimer;
        private readonly ICameraControls        _cameraControls;


        private readonly string _failScreenKey;
        private readonly string _hudScreenKey;

        public IGroupService   Group        { get; private set; }
        public IObjectResolver Resolver     { get; private set; }
        public IStagePooling   StagePooling { get; private set; }


        public StageManager(
            IGroupService          group,
            IObjectResolver        resolver,
            IStageEntityWorld      entityWorld,
            IStagePooling          stagePooling,
            IScreenManager         screenManager,
            CinemachineTargetGroup targetGroup,
            IGameTimer             gameTimer,
            ICameraControls        cameraControls,
            string                 failScreenKey,
            string                 hudScreenKey
        ) {
            Group           = group;
            Resolver        = resolver;
            _entityWorld    = entityWorld;
            StagePooling    = stagePooling;
            _screenManager  = screenManager;
            _targetGroup    = targetGroup;
            _failScreenKey  = failScreenKey;
            _hudScreenKey   = hudScreenKey;
            _gameTimer      = gameTimer;
            _cameraControls = cameraControls;
        }
    }
}