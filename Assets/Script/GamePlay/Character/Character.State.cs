using Script.GameInfo.Character;

namespace Script.GamePlay.Character {
    public partial class Character {
        // State 변환
        public void SetState(CharacterState state) {
            if (State.Value.HasFlag(state)) return;
            State.OnNext(state);
        }

        // State 추가
        public void AddState(CharacterState state, bool notify = true) {
            if (State.Value.HasFlag(state)) return;
            if (notify)
                State.OnNext(State.Value |= state);
            else {
                State.Value |= state;
            }
        }

        public void RemoveState(CharacterState state, bool notify = true) {
            if (State.Value.HasFlag(state) == false) return;
            if (notify)
                State.OnNext(State.Value &= ~state);
            else {
                State.Value |= state;
            }
        }
    }
}