using Script.GamePlay.Audio.Interface;
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
        private readonly CinemachineCamera      _vCamera;
        private readonly IStageEntityWorld      _entityWorld;
        private readonly IScreenManager         _screenManager;
        private readonly IGameTimer             _gameTimer;
        private readonly IAudioManager          _audioManager;


        private readonly string _failScreenKey;
        private readonly string _hudScreenKey;
        private readonly string _clearScreenKey;
        private readonly string _countDownKey;

        public IGroupService   Group          { get; private set; }
        public IObjectResolver Resolver       { get; private set; }
        public IStagePooling   StagePooling   { get; private set; }
        public ICameraControls CameraControls { get; private set; }


        public StageManager(
            IGroupService          group,
            IObjectResolver        resolver,
            IStageEntityWorld      entityWorld,
            IStagePooling          stagePooling,
            IScreenManager         screenManager,
            CinemachineTargetGroup targetGroup,
            IGameTimer             gameTimer,
            ICameraControls        cameraControls,
            CinemachineCamera      vCamera,
            IAudioManager          audioManager,
            string                 failScreenKey,
            string                 hudScreenKey,
            string                 clearScreenKey,
            string                 countDownKey
        ) {
            Group           = group;
            Resolver        = resolver;
            _entityWorld    = entityWorld;
            StagePooling    = stagePooling;
            _screenManager  = screenManager;
            _targetGroup    = targetGroup;
            _failScreenKey  = failScreenKey;
            _hudScreenKey   = hudScreenKey;
            _clearScreenKey = clearScreenKey;
            _countDownKey   = countDownKey;
            _gameTimer      = gameTimer;
            CameraControls  = cameraControls;
            _vCamera        = vCamera;
            _audioManager    = audioManager;
        }
    }
}