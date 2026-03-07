using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MessagePack;
using UnityEngine;

namespace Expression {
    public enum Operator {
        PushConst,
        PushVar,

        Add,
        Sub,
        Mul,
        Div,

        Neg,

        CallFunc // 함수 호출
    }

    /// <summary>
    /// 컴파일된(불변) 수식. 생성 시 컴파일되고, 런타임에서는 Calc만 호출.
    /// </summary>
    [MessagePackObject(AllowPrivate = true)]
    [System.Serializable]
    public partial class Expression {
        [IgnoreMember]
        public ExpressionValue[] Code => _code;

        [IgnoreMember]
        public int MaxStack => _maxStack;

        [SerializeField]
        [Key(0)] private ExpressionValue[] _code;

        [SerializeField]
        [Key(1)] private int _maxStack;


        public static implicit operator Expression(string s) => new(s);

        public Expression(string expression) {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            CompileInto(expression.AsSpan(), out _code, out _maxStack);
        }

        public Expression(ReadOnlySpan<char> expression) {
            CompileInto(expression, out _code, out _maxStack);
        }

        public Expression(ExpressionValue[] code, int maxStack) {
            _code     = code ?? throw new ArgumentNullException(nameof(code));
            _maxStack = Math.Max(1, maxStack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Calc() {
            Span<double> stack = _maxStack <= 256
                                     ? stackalloc double[_maxStack]
                                     : new double[_maxStack];

            int sp = 0;

            foreach (var t in _code) {
                ref readonly var ins = ref t;

                switch (ins.Op) {
                    case Operator.PushConst:
                        stack[sp++] = ins.Const;
                        break;

                    case Operator.PushVar: {
                        if (!ValueContext.TryGetValue(ins.Key, out double v))
                            v = 0; // 정책: 없으면 0
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


        private enum OperatorKind : byte {
            Plus,
            Minus,
            Star,
            Slash,
            Neg, // 단항 -
            LParen,
            Func // 함수 스택 항목
        }

        private readonly struct OperatorEntry {
            public readonly OperatorKind Kind;
            public readonly ExprFunction Func;

            public OperatorEntry(OperatorKind kind, ExprFunction func = 0) {
                Kind = kind;
                Func = func;
            }
        }

        private static int Prec(OperatorKind op)
            => op switch {
                OperatorKind.Neg                        => 3,
                OperatorKind.Star or OperatorKind.Slash => 2,
                OperatorKind.Plus or OperatorKind.Minus => 1,
                _                                       => 0
            };

        private static void CompileInto(ReadOnlySpan<char> expression, out ExpressionValue[] code, out int maxStack) {
            var tokens = Tokenizer.Tokenize(expression, out int tokenCount);

            var output       = new List<ExpressionValue>(tokenCount * 2);
            var ops          = new Stack<OperatorEntry>(Math.Max(8, tokenCount));
            var funcArgCount = new Stack<int>();

            var prevCanBeUnary  = true;
            var sp              = 0;
            var maxSp           = 0;
            var pendingFuncCall = false;

            for (int i = 0; i < tokenCount; i++) {
                var t = tokens[i];
                if (t.Kind == Tokenizer.TokenKind.End) break;

                switch (t.Kind) {
                    case Tokenizer.TokenKind.Number:
                        output.Add(ExpressionValue.ConstVal(t.Number));
                        sp++;
                        if (sp > maxSp) maxSp = sp;
                        prevCanBeUnary = false;
                        break;

                    case Tokenizer.TokenKind.Var:
                        output.Add(ExpressionValue.Var(t.Key));
                        sp++;
                        if (sp > maxSp) maxSp = sp;
                        prevCanBeUnary = false;
                        break;

                    case Tokenizer.TokenKind.Func:
                        ops.Push(new OperatorEntry(OperatorKind.Func, t.Func));
                        pendingFuncCall = true;
                        prevCanBeUnary  = true;
                        break;

                    case Tokenizer.TokenKind.LParen:
                        ops.Push(new OperatorEntry(OperatorKind.LParen));

                        if (pendingFuncCall) {
                            funcArgCount.Push(0); // comma count
                            pendingFuncCall = false;
                        }

                        prevCanBeUnary = true;
                        break;

                    case Tokenizer.TokenKind.Comma:
                        while (ops.Count > 0 && ops.Peek().Kind != OperatorKind.LParen)
                            EmitOp(ops.Pop(), output, ref sp);

                        if (funcArgCount.Count == 0)
                            throw new FormatException("Comma outside function call.");

                        funcArgCount.Push(funcArgCount.Pop() + 1);
                        prevCanBeUnary = true;
                        break;

                    case Tokenizer.TokenKind.RParen:
                        while (ops.Count > 0 && ops.Peek().Kind != OperatorKind.LParen)
                            EmitOp(ops.Pop(), output, ref sp);

                        if (ops.Count == 0 || ops.Peek().Kind != OperatorKind.LParen)
                            throw new FormatException("Mismatched parentheses.");

                        ops.Pop(); // pop '('

                        if (ops.Count > 0 && ops.Peek().Kind == OperatorKind.Func) {
                            var f = ops.Pop().Func;

                            if (funcArgCount.Count == 0)
                                throw new FormatException("Function arg tracking error.");

                            int commaCount = funcArgCount.Pop();
                            int arity      = commaCount + 1;

                            ValidateArity(f, arity);

                            output.Add(ExpressionValue.Call(f, (byte)arity));
                            sp -= (arity - 1);
                        }

                        prevCanBeUnary = false;
                        break;

                    case Tokenizer.TokenKind.Plus:
                    case Tokenizer.TokenKind.Minus:
                    case Tokenizer.TokenKind.Star:
                    case Tokenizer.TokenKind.Slash: {
                        bool unaryMinus = (t.Kind == Tokenizer.TokenKind.Minus) && prevCanBeUnary;

                        OperatorKind cur = unaryMinus
                                               ? OperatorKind.Neg
                                               : t.Kind switch {
                                                   Tokenizer.TokenKind.Plus  => OperatorKind.Plus,
                                                   Tokenizer.TokenKind.Minus => OperatorKind.Minus,
                                                   Tokenizer.TokenKind.Star  => OperatorKind.Star,
                                                   Tokenizer.TokenKind.Slash => OperatorKind.Slash,
                                                   _                         => throw new InvalidOperationException()
                                               };

                        int curPrec = Prec(cur);

                        while (ops.Count > 0) {
                            var top = ops.Peek();
                            if (top.Kind == OperatorKind.LParen || top.Kind == OperatorKind.Func) break;

                            int topPrec = Prec(top.Kind);

                            bool shouldPop =
                                (cur != OperatorKind.Neg && topPrec >= curPrec) || (cur == OperatorKind.Neg && topPrec > curPrec);

                            if (!shouldPop) break;

                            EmitOp(ops.Pop(), output, ref sp);
                        }

                        ops.Push(new OperatorEntry(cur));
                        prevCanBeUnary = true;
                        break;
                    }

                    default:
                        throw new FormatException($"Unexpected token: {t.Kind}");
                }
            }

            while (ops.Count > 0) {
                var op = ops.Pop();
                if (op.Kind == OperatorKind.LParen) throw new FormatException("Mismatched parentheses.");
                if (op.Kind == OperatorKind.Func) throw new FormatException("Dangling function.");
                EmitOp(op, output, ref sp);
            }

            code     = output.ToArray();
            maxStack = Math.Max(1, maxSp);
        }

        private static void EmitOp(OperatorEntry op, List<ExpressionValue> output, ref int sp) {
            switch (op.Kind) {
                case OperatorKind.Plus:
                    output.Add(ExpressionValue.OpOnly(Operator.Add));
                    sp--;
                    break;
                case OperatorKind.Minus:
                    output.Add(ExpressionValue.OpOnly(Operator.Sub));
                    sp--;
                    break;
                case OperatorKind.Star:
                    output.Add(ExpressionValue.OpOnly(Operator.Mul));
                    sp--;
                    break;
                case OperatorKind.Slash:
                    output.Add(ExpressionValue.OpOnly(Operator.Div));
                    sp--;
                    break;
                case OperatorKind.Neg: output.Add(ExpressionValue.OpOnly(Operator.Neg)); break;
                default:
                    throw new InvalidOperationException($"Invalid op kind: {op.Kind}");
            }
        }

        private static void ValidateArity(ExprFunction func, int arity) {
            switch (func) {
                case ExprFunction.Pow:
                    if (arity != 2) throw new FormatException("Pow(a,b) requires exactly 2 args.");
                    break;

                case ExprFunction.Log:
                    if (arity != 1 && arity != 2) throw new FormatException("Log(x) or Log(x, base) only.");
                    break;

                case ExprFunction.Min:
                case ExprFunction.Max:
                    if (arity < 2) throw new FormatException($"{func} requires at least 2 args.");
                    break;

                default:
                    throw new FormatException($"Unknown function: {func}");
            }
        }
    }
}