using System;
using System.Globalization;

namespace Expression {
    public static class Tokenizer {
        public enum TokenKind : byte {
            Number,
            Var,  // 변수 (key)
            Func, // 함수 (funcId)
            Plus,
            Minus,
            Star,
            Slash,
            LParen,
            RParen,
            Comma,
            End
        }

        public struct Token {
            public readonly TokenKind    Kind;
            public readonly double       Number;
            public readonly int          Key;
            public readonly ExprFunction Func;

            private Token(TokenKind kind, double number, int key, ExprFunction func) {
                Kind   = kind;
                Number = number;
                Key    = key;
                Func   = func;
            }

            public static Token Num(double           v)   => new(TokenKind.Number, v, 0, 0);
            public static Token Var(int              key) => new(TokenKind.Var, 0, key, 0);
            public static Token FuncTok(ExprFunction f)   => new(TokenKind.Func, 0, 0, f);
            public static Token Sym(TokenKind        k)   => new(k, 0, 0, 0);
        }

        public static Token[] Tokenize(ReadOnlySpan<char> src, out int count) {
            var tokens = new Token[Math.Max(8, src.Length)];
            var n      = 0;

            var i = 0;
            while (i < src.Length) {
                var c = src[i];

                if (char.IsWhiteSpace(c)) {
                    i++;
                    continue;
                }

                // number
                if (char.IsDigit(c) || c == '.') {
                    var start = i++;
                    while (i < src.Length) {
                        var ch = src[i];
                        if (char.IsDigit(ch) || ch == '.' || ch == 'e' || ch == 'E' || ch == '+' || ch == '-') {
                            i++;
                            continue;
                        }

                        break;
                    }

                    var slice = src.Slice(start, i - start);
                    if (!double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                        throw new FormatException($"Invalid number: '{slice.ToString()}'");

                    tokens[n++] = Token.Num(v);
                    continue;
                }

                // ident -> func or var
                if (char.IsLetter(c) || c == '_') {
                    var start = i++;
                    while (i < src.Length) {
                        var ch = src[i];
                        if (char.IsLetterOrDigit(ch) || ch == '_') i++;
                        else break;
                    }

                    ReadOnlySpan<char> name = src.Slice(start, i - start);

                    // lookahead for '('
                    var j = i;
                    while (j < src.Length && char.IsWhiteSpace(src[j])) j++;
                    var isFuncCall = j < src.Length && src[j] == '(';

                    if (isFuncCall) {
                        if (!ExprFunctionUtil.TryParse(name, out var func))
                            throw new FormatException($"Unknown function '{name.ToString()}'");

                        tokens[n++] = Token.FuncTok(func);
                    }
                    else {
                        int key = ValueStringKey.GetKey(name);
                        tokens[n++] = Token.Var(key);
                    }

                    continue;
                }

                // symbols
                tokens[n++] = c switch {
                    '+' => Token.Sym(TokenKind.Plus),
                    '-' => Token.Sym(TokenKind.Minus),
                    '*' => Token.Sym(TokenKind.Star),
                    '/' => Token.Sym(TokenKind.Slash),
                    '(' => Token.Sym(TokenKind.LParen),
                    ')' => Token.Sym(TokenKind.RParen),
                    ',' => Token.Sym(TokenKind.Comma),
                    _   => throw new FormatException($"Unexpected char '{c}' at index {i}")
                };

                i++;
            }

            tokens[n++] = Token.Sym(TokenKind.End);
            count       = n;
            return tokens;
        }
    }
}