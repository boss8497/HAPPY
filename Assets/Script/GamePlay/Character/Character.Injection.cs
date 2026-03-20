using Script.GamePlay.Input;
using Script.GamePlay.Service.Interface;
using VContainer;


// VContainer에서 주입 받을 코드
namespace Script.GamePlay.Character {
    public partial class Character {
        private IPlayerControls _playerControls;
        private IGroupService   _groupService;

        [Inject]
        public void Constructor(
            IPlayerControls playerControls,
            IGroupService   groupService
        ) {
            _playerControls = playerControls;
            _groupService   = groupService;
        }


        public IPlayerControls PlayerControls => _playerControls;
    }
}