using Script.GamePlay.ECS.World;
using Script.GamePlay.Input;
using Script.GamePlay.Service.Interface;
using Script.GamePlay.Stage;
using Script.GamePlay.Unit.Interface;
using VContainer;


// VContainer에서 주입 받을 코드
namespace Script.GamePlay.Character {
    public partial class Character {
        private IStageManager     _stageManager;
        private IPlayerControls   _playerControls;
        private IUnitManager      _unitManager;
        private IGroupService     _groupService;
        private IObjectResolver   _resolver;
        private IStageEntityWorld _stageEntityWorld;

        [Inject]
        public void Constructor(
            IStageManager     stageManager,
            IPlayerControls   playerControls,
            IUnitManager      unitManager,
            IGroupService     groupService,
            IObjectResolver   resolver,
            IStageEntityWorld stageEntityWorld
        ) {
            _stageManager     = stageManager;
            _playerControls   = playerControls;
            _unitManager      = unitManager;
            _groupService     = groupService;
            _resolver         = resolver;
            _stageEntityWorld = stageEntityWorld;
        }


        public IPlayerControls PlayerControls => _playerControls;
        public IGroupService   GroupService   => _groupService;
        public IObjectResolver Resolver       => _resolver;
    }
}