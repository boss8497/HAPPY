using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    public partial class Character {
        private CharacterState _state = CharacterState.None;
        public CharacterState State  => _state;


        public bool Jumping       => (_state & CharacterState.Jumping) != 0;
        public bool Running       => (_state & CharacterState.Running) != 0;
        public bool Die           => (_state & CharacterState.Die) != 0;
        public bool Initialized   => (_state & CharacterState.Initialized) != 0;
        public bool SystemControl => !Initialized || (_state & CharacterState.SystemControl) != 0;

        
        public void AddState(CharacterState state) {
            if (_state.HasFlag(state)) return;
            _state |= state;
            SyncCharacterHitboxEntity();
        }

        public void RemoveState(CharacterState state) {
            if (_state.HasFlag(state) == false) return;
            _state &= ~state;
            SyncCharacterHitboxEntity();
        }
        
    }
}