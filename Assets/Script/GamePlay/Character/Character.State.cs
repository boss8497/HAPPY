using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    public partial class Character {
        // State 변환
        public void SetState(CharacterState state) {
            if (State.Value.HasFlag(state)) return;
            State.OnNext(state);
            SyncCharacterHitboxEntity();
        }
        // State 추가
        public void AddState(CharacterState state) {
            if (State.Value.HasFlag(state)) return;
            State.OnNext(State.Value |= state);
            SyncCharacterHitboxEntity();
        }

        public void RemoveState(CharacterState state) {
            if (State.Value.HasFlag(state) == false) return;
            State.OnNext(State.Value &= ~state);
            SyncCharacterHitboxEntity();
        }
        
    }
}