using System.Runtime.CompilerServices;

namespace Expression {
    public readonly struct ExprInstruction {
        public readonly Operator Op;

        // PushConst
        public readonly double Const;

        // PushVar
        public readonly int Key;

        // CallFunc
        public readonly ExprFunction Func;
        public readonly byte         Arity; // 인자 개수 (Min/Max 가변 포함)

        private ExprInstruction(Operator op, double c, int key, ExprFunction func, byte arity) {
            Op    = op;
            Const = c;
            Key   = key;
            Func  = func;
            Arity = arity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExprInstruction ConstVal(double v) => new(Operator.PushConst, v, 0, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExprInstruction Var(int key) => new(Operator.PushVar, 0, key, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExprInstruction OpOnly(Operator op) => new(op, 0, 0, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExprInstruction Call(ExprFunction func, byte arity) => new(Operator.CallFunc, 0, 0, func, arity);
    }
}