using Script.GamePlay.Service.Interface;
using Unity.Cinemachine;
using VContainer;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private readonly IGroupService          _group;
        private readonly IObjectResolver        _resolver;
        private readonly CinemachineTargetGroup _targetGroup;


        public StageManager(
            IGroupService          group,
            IObjectResolver        resolver,
            CinemachineTargetGroup targetGroup
        ) {
            _group       = group;
            _resolver    = resolver;
            _targetGroup = targetGroup;
            Test().Forget();
        }

        public IGroupService   Group    => _group;
        public IObjectResolver Resolver => _resolver;
    }
}