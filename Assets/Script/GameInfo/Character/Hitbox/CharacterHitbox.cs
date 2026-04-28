
using Sirenix.OdinInspector;

namespace Script.GameInfo.Character {
    [System.Serializable]
    public class CharacterHitbox {
        public CharacterState state;
        public Hitbox         hitbox = new();
        [LabelText("우선순위")]
        [PropertyTooltip("상태가 겹칠 때 우선 선택할 우선 순위. 숫자가 커지면 우선순위가 높다.")]
        public int priority = 0;
    }
}