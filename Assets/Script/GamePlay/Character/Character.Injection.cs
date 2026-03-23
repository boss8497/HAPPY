using Script.GamePlay.Input;
using Script.GamePlay.Service.Interface;
using VContainer;


// VContainer에서 주입 받을 코드
namespace Script.GamePlay.Character {
    public partial class Character {
        private IPlayerControls _playerControls;
        private IGroupService   _groupService;
        private IObjectResolver _resolver;

        [Inject]
        public void Constructor(
            IPlayerControls playerControls,
            IGroupService   groupService,
            IObjectResolver resolver
        ) {
            _playerControls = playerControls;
            _groupService   = groupService;
            _resolver       = resolver;
        }


        public IPlayerControls PlayerControls => _playerControls;
        public IGroupService   GroupService   => _groupService;
        public IObjectResolver Resolver       => _resolver;
    }
}