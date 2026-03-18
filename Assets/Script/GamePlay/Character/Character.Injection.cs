using Script.GamePlay.Input;
using VContainer;


// VContainer에서 주입 받을 코드
namespace Script.GamePlay.Character {
    public partial class Character {
        private IPlayerControls _playerControls;

        [Inject]
        public void Constructor(
            IPlayerControls playerControls
        ) {
            _playerControls = playerControls;
        }


        public IPlayerControls PlayerControls => _playerControls;
    }
}