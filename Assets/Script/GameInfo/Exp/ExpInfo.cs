using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Info.Enum;
using UnityEngine;

namespace Script.GameInfo {
    [AutoEditorTable(true)]
    [System.Serializable]
    public class ExpInfo : InfoBase {
        public LevelType levelType;

        [Item]
        public int itemUid;
        
        [field: SerializeField]
        public Expression.Expression expression = new Expression.Expression("0");

        public double Calc() {
            if (expression == null)
                return 0d;

            return expression.Calc();
        }
    }
}