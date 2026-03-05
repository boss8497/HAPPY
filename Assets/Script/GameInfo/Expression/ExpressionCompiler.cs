using System;
using System.Collections.Generic;

namespace Expression {
    public static class ExpressionCompiler {
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
            public readonly OperatorKind       Kind;
            public readonly ExprFunction Func;

            public OperatorEntry(OperatorKind kind, ExprFunction func = 0) {
                Kind = kind;
                Func = func;
            }
        }

        private static int Prec(OperatorKind @operator)
            => @operator switch {
                OperatorKind.Neg                  => 3,
                OperatorKind.Star or OperatorKind.Slash => 2,
                OperatorKind.Plus or OperatorKind.Minus => 1,
                _                           => 0
            };

        public static Expression Compile(string expression) {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            return Compile(expression.AsSpan());
        }

        public static Expression Compile(ReadOnlySpan<char> expression) {
            var tokens = Tokenizer.Tokenize(expression, out int tokenCount);

            var output = new List<ExprInstruction>(tokenCount * 2);
            var ops    = new Stack<OperatorEntry>(Math.Max(8, tokenCount));

            // 함수 인자 카운트 스택: "함수 호출의 '('를 만났을 때" push
            // comma를 만날 때마다 ++, ')'에서 최종 arity 결정
            var funcArgCount = new Stack<int>();

            bool prevCanBeUnary = true; // 시작/연산자/'(' 다음의 '-'는 unary 가능

            // max stack depth 계산
            int sp = 0, maxSp = 0;

            for (int i = 0; i < tokenCount; i++) {
                var t = tokens[i];
                if (t.Kind == Tokenizer.TokenKind.End) break;

                switch (t.Kind) {
                    case Tokenizer.TokenKind.Number:
                        output.Add(ExprInstruction.ConstVal(t.Number));
                        sp++;
                        if (sp > maxSp) maxSp = sp;
                        prevCanBeUnary = false;
                        break;

                    case Tokenizer.TokenKind.Var:
                        output.Add(ExprInstruction.Var(t.Key));
                        sp++;
                        if (sp > maxSp) maxSp = sp;
                        prevCanBeUnary = false;
                        break;

                    case Tokenizer.TokenKind.Func:
                        // 함수 이름을 ops에 넣어두고, 다음 '('를 만나면 실제 호출 시작
                        ops.Push(new OperatorEntry(OperatorKind.Func, t.Func));
                        prevCanBeUnary = true;
                        break;

                    case Tokenizer.TokenKind.LParen:
                        ops.Push(new OperatorEntry(OperatorKind.LParen));

                        // 바로 직전에 Func가 있었다면, 이 '('는 함수 호출의 시작
                        // (토크나이저가 Func로 만들어준 시점에서 이미 다음이 '('인 상황)
                        if (ops.Count >= 2) {
                            // 스택 top은 LParen, 그 아래가 Func인지 체크하려면 일단 pop/push로 확인하는 방식이 필요하지만
                            // 여기서는 "Func 토큰을 보면 ops에 Func가 push"되고, 곧바로 '('가 오므로
                            // funcArgCount는 '('가 왔을 때, 스택에 Func가 존재하면 push한다고 처리한다.

                            // ops.Peek()는 LParen이므로, 아래 원소 확인은 어렵다.
                            // 따라서 더 간단하게: LParen 토큰 처리 직전에, "이 '('가 함수 호출인지"를 파서 상태로 추적
                            // -> 토크나이저가 Func 토큰을 별도로 줬으니, prevCanBeUnary==true 같은 걸로 판단하면 위험
                            // 깔끔하게: 직전 토큰이 Func였는지를 별도 변수로.
                        }

                        prevCanBeUnary = true;
                        break;

                    case Tokenizer.TokenKind.Comma:
                        // 콤마는 "현재 함수 호출의 인자 구분"이므로, '('까지 연산자들을 출력
                        while (ops.Count > 0 && ops.Peek().Kind != OperatorKind.LParen)
                            EmitOp(ops.Pop(), output, ref sp);

                        if (funcArgCount.Count == 0)
                            throw new FormatException("Comma outside function call.");

                        funcArgCount.Push(funcArgCount.Pop() + 1); // 인자 하나 더 늘어남
                        prevCanBeUnary = true;
                        break;

                    case Tokenizer.TokenKind.RParen:
                        while (ops.Count > 0 && ops.Peek().Kind != OperatorKind.LParen)
                            EmitOp(ops.Pop(), output, ref sp);

                        if (ops.Count == 0 || ops.Peek().Kind != OperatorKind.LParen)
                            throw new FormatException("Mismatched parentheses.");

                        ops.Pop(); // pop '('

                        // '('를 닫았는데, 바로 위에 함수가 있으면 함수 호출 완료
                        if (ops.Count > 0 && ops.Peek().Kind == OperatorKind.Func) {
                            var f = ops.Pop().Func;

                            // 인자 개수 계산:
                            // funcArgCount에는 "comma 수"가 들어가고, 인자가 하나라도 있으면 (comma+1)
                            // 단, "Func(" 다음 바로 ")"면 0 인자 -> 여기서는 허용하지 않음
                            if (funcArgCount.Count == 0)
                                throw new FormatException("Function arg tracking error.");

                            int commaCount = funcArgCount.Pop();
                            int arity      = commaCount + 1;

                            ValidateArity(f, arity);

                            output.Add(ExprInstruction.Call(f, (byte)arity));
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
                                             _                             => throw new InvalidOperationException()
                                         };

                        int curPrec = Prec(cur);

                        // left-assoc: 같은 우선순위면 pop (Neg는 right-assoc처럼 처리해서 같은 우선순위면 pop하지 않게 해도 됨)
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

                // ✅ 함수 호출 시작 트래킹:
                // 토크나이저가 Func를 따로 만들었으니, "Func 토큰 다음에 오는 LParen"에서 인자 카운트 push 해야 함.
                // 가장 읽기 쉬운 방법: i를 보고 다음 토큰이 LParen인지, 현재 토큰이 Func인지일 때, 다음 루프에서 LParen 처리하기 전에 push하는 건 꼬여서
                // 여기서는 "LParen을 처리한 직후에, 바로 아래에 Func가 존재하면 push"가 필요하지만 Stack은 아래 확인이 불편함.
                // 그래서 더 단순하게: "Func 토큰을 ops에 push한 다음, 바로 다음 토큰이 LParen이면 인자 카운트 0 push"를 여기서 처리.
                if (t.Kind == Tokenizer.TokenKind.Func) {
                    // 다음 non-End 토큰이 LParen이어야 정상 함수 호출
                    int next = i + 1;
                    while (next < tokenCount && tokens[next].Kind == Tokenizer.TokenKind.End) next++;
                    if (next < tokenCount && tokens[next].Kind == Tokenizer.TokenKind.LParen) {
                        funcArgCount.Push(0); // comma count
                    }
                }
            }

            while (ops.Count > 0) {
                var op = ops.Pop();
                if (op.Kind == OperatorKind.LParen)
                    throw new FormatException("Mismatched parentheses.");
                if (op.Kind == OperatorKind.Func)
                    throw new FormatException("Dangling function.");
                EmitOp(op, output, ref sp);
            }

            return new Expression(output.ToArray(), maxSp);
        }

        private static void EmitOp(OperatorEntry @operator, List<ExprInstruction> output, ref int sp) {
            switch (@operator.Kind) {
                case OperatorKind.Plus:
                    output.Add(ExprInstruction.OpOnly(Operator.Add));
                    sp--;
                    break;
                case OperatorKind.Minus:
                    output.Add(ExprInstruction.OpOnly(Operator.Sub));
                    sp--;
                    break;
                case OperatorKind.Star:
                    output.Add(ExprInstruction.OpOnly(Operator.Mul));
                    sp--;
                    break;
                case OperatorKind.Slash:
                    output.Add(ExprInstruction.OpOnly(Operator.Div));
                    sp--;
                    break;

                case OperatorKind.Neg:
                    output.Add(ExprInstruction.OpOnly(Operator.Neg));
                    // sp 변화 없음
                    break;

                default:
                    throw new InvalidOperationException($"Invalid op kind: {@operator.Kind}");
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