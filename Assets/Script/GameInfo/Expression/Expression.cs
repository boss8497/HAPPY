using System;
using System.Runtime.CompilerServices;

namespace Expression {
    public enum Operator {
        PushConst,
        PushVar,

        Add,
        Sub,
        Mul,
        Div,

        Neg,

        CallFunc // 함수 호출 (arity + funcId)
    }
    /// <summary>
    /// 컴파일된(불변) 수식. 런타임에서는 Calc만 호출.
    /// </summary>
    public sealed class Expression {
        public           ExprInstruction[] Code     => _code;
        public           int               MaxStack => _maxStack;
        
        private readonly ExprInstruction[] _code;
        private readonly int               _maxStack;

        public Expression(ExprInstruction[] code, int maxStack) {
            _code     = code ?? throw new ArgumentNullException(nameof(code));
            _maxStack = Math.Max(1, maxStack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Calc() {
            Span<double> stack = _maxStack <= 256
                                     ? stackalloc double[_maxStack]
                                     : new double[_maxStack]; // 매우 긴 수식은 드물다고 가정

            var sp = 0;

            foreach (var t in _code) {
                ref readonly var ins = ref t;

                switch (ins.Op) {
                    case Operator.PushConst:
                        stack[sp++] = ins.Const;
                        break;

                    case Operator.PushVar: {
                        if (!ValueContext.TryGetValue(ins.Key, out double v))
                            v = 0; // 정책: 없으면 0 (원하면 throw로 변경)
                        stack[sp++] = v;
                        break;
                    }

                    case Operator.Add:
                        stack[sp - 2] = stack[sp - 2] + stack[sp - 1];
                        sp--;
                        break;
                    case Operator.Sub:
                        stack[sp - 2] = stack[sp - 2] - stack[sp - 1];
                        sp--;
                        break;
                    case Operator.Mul:
                        stack[sp - 2] = stack[sp - 2] * stack[sp - 1];
                        sp--;
                        break;
                    case Operator.Div:
                        stack[sp - 2] = stack[sp - 2] / stack[sp - 1];
                        sp--;
                        break;

                    case Operator.Neg:
                        stack[sp - 1] = -stack[sp - 1];
                        break;

                    case Operator.CallFunc: {
                        int n = ins.Arity;
                        if (n <= 0) throw new FormatException("Invalid function arity.");
                        if (sp < n) throw new FormatException("Invalid expression (stack underflow).");

                        double result = EvalFunction(ins.Func, stack.Slice(sp - n, n));
                        sp          -= n;
                        stack[sp++] =  result;
                        break;
                    }

                    default:
                        throw new InvalidOperationException($"Unknown opcode: {ins.Op}");
                }
            }

            if (sp != 1) throw new FormatException("Invalid expression (stack not balanced).");
            return stack[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double EvalFunction(ExprFunction func, ReadOnlySpan<double> args) {
            switch (func) {
                case ExprFunction.Pow:
                    if (args.Length != 2) throw new FormatException("Pow(a,b) requires 2 args.");
                    return Math.Pow(args[0], args[1]);

                case ExprFunction.Log:
                    if (args.Length == 1) return Math.Log(args[0]);
                    if (args.Length == 2) return Math.Log(args[0], args[1]);
                    throw new FormatException("Log(x) or Log(x, base) supported.");

                case ExprFunction.Min:
                    if (args.Length < 2) throw new FormatException("Min requires at least 2 args.");
                {
                    double m = args[0];
                    for (int i = 1; i < args.Length; i++)
                        if (args[i] < m)
                            m = args[i];
                    return m;
                }

                case ExprFunction.Max:
                    if (args.Length < 2) throw new FormatException("Max requires at least 2 args.");
                {
                    double m = args[0];
                    for (int i = 1; i < args.Length; i++)
                        if (args[i] > m)
                            m = args[i];
                    return m;
                }

                default:
                    throw new FormatException($"Unknown function: {func}");
            }
        }
    }
}