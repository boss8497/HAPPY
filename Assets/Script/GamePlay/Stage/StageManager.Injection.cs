using Script.GamePlay.ECS.World;
using Script.GamePlay.Pool;
using Script.GamePlay.Service.Interface;
using Script.GUI.Screen.Interface;
using Unity.Cinemachine;
using VContainer;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private readonly CinemachineTargetGroup _targetGroup;
        private readonly IStageEntityWorld      _entityWorld;
        private readonly IScreenManager         _screenManager;


        public IGroupService   Group        { get; private set; }
        public IObjectResolver Resolver     { get; private set; }
        public IStagePooling   StagePooling { get; private set; }


        public StageManager(
            IGroupService          group,
            IObjectResolver        resolver,
            CinemachineTargetGroup targetGroup,
            IStageEntityWorld      entityWorld,
            IStagePooling          stagePooling,
            IScreenManager         screenManager
        ) {
            Group          = group;
            Resolver       = resolver;
            _targetGroup   = targetGroup;
            _entityWorld   = entityWorld;
            StagePooling   = stagePooling;
            _screenManager = screenManager;
        }
    }
}