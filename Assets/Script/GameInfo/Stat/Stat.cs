using Script.GameInfo.Info.Enum;
using UnityEngine;

namespace Script.GameInfo.Info.Stat {
    [System.Serializable]
    public class Stat {
        public StatType              type;
        public bool                  isPercent;
        
        [field: SerializeField]
        public Expression.Expression expression = new Expression.Expression("0");

        /// <summary>
        /// 수식을 계산해서 값 반환
        /// Expression 내부에서 level 등 필요한 값을 참조한다고 가정
        /// </summary>
        public double Calc() {
            if (expression == null)
                return 0d;

            return expression.Calc();
        }
    }
}