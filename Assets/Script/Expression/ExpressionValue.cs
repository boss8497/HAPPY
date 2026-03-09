using System.Runtime.CompilerServices;
using MessagePack;

namespace Expression {
    [MessagePackObject]
    [System.Serializable]
    public partial struct ExpressionValue {
        [Key(0)] public Operator Op;
        // PushConst
        [Key(1)] public double Const;
        // PushVar
        [Key(2)] public int Key;
        // CallFunc
        [Key(3)] public ExprFunction Func;
        [Key(4)] public byte         Arity; // 인자 개수 (Min/Max 가변 포함)

        private ExpressionValue(Operator op, double c, int key, ExprFunction func, byte arity) {
            Op    = op;
            Const = c;
            Key   = key;
            Func  = func;
            Arity = arity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionValue ConstVal(double v) => new(Operator.PushConst, v, 0, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionValue Var(int key) => new(Operator.PushVar, 0, key, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionValue OpOnly(Operator op) => new(op, 0, 0, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExpressionValue Call(ExprFunction func, byte arity) => new(Operator.CallFunc, 0, 0, func, arity);
    }
}