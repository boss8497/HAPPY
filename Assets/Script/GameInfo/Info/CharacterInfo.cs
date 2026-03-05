using Script.GameInfo.Base;
using Expression;
using UnityEngine;

namespace Script.GameInfo.Info {
    [System.Serializable]
    public class CharacterInfo : InfoBase {
        [field: SerializeField]
        public Expression.Expression Test = new("1 + 2 * 3");
    }
}