using System;

namespace Expression {
    public enum ExprFunction : byte {
        Pow,
        Log, // Log(x) or Log(x, base)
        Min, // Min(a,b,...) 2개 이상
        Max  // Max(a,b,...) 2개 이상
    }

    public static class ExprFunctionUtil {
        // 함수 이름: 보통 대/소문자 섞여 들어오므로 OrdinalIgnoreCase로 비교
        public static bool TryParse(ReadOnlySpan<char> name, out ExprFunction func) {
            // 작은 함수 몇 개만 지원하니 switch 스타일로 빠르고 간단하게
            if (name.Equals("pow".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
                func = ExprFunction.Pow;
                return true;
            }

            if (name.Equals("log".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
                func = ExprFunction.Log;
                return true;
            }

            if (name.Equals("min".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
                func = ExprFunction.Min;
                return true;
            }

            if (name.Equals("max".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
                func = ExprFunction.Max;
                return true;
            }

            func = default;
            return false;
        }
    }
}