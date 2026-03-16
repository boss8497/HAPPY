#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Script.GameInfo.Expression.Editor {
    /// <summary>
    /// Expression(컴파일 결과만 저장)을 Odin에서
    /// - 현재 컴파일된 코드를 사람이 읽기 쉬운 문자열로 "역표시(Decompile)"
    /// - 사용자가 수정
    /// - Compile 버튼으로 재컴파일하여 _code/_maxStack만 갱신
    /// 하는 Drawer.
    /// 
    /// 원문 string을 Expression에 저장하지 않는다.
    /// </summary>
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public sealed class ExpressionDrawer : OdinValueDrawer<global::Expression.Expression> {
        // Drawer 인스턴스가 여러 프로퍼티에 재사용될 수 있어서 per-property 캐시 필요
        private static readonly Dictionary<int, State> States = new(256);

        private sealed class State {
            public string EditText;       // 사용자가 편집중인 문자열
            public string LastDecompiled; // 현재 Expression에서 역표시한 문자열
            public string LastError;      // 컴파일 에러
            public int    LastCodeHash;   // code 변경 감지

            public bool ShowDisasm = false;
            public bool ShowHelp   = false;
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            var prop  = Property;
            var entry = ValueEntry;

            // null일 수 있으면 기본값 하나 만들어둠(빈 Expression 허용 여부는 너 정책에 따라)
            // 지금 Expression(string)에서 null만 막고 빈 문자열은 토크나이저가 End만 생성 → 코드 empty일 수 있음
            var expr = entry.SmartValue ?? (entry.SmartValue = new global::Expression.Expression("0"));

            int key = MakeStateKey(prop);
            if (!States.TryGetValue(key, out var st)) {
                st          = new State();
                States[key] = st;
            }

            // 현재 code로부터 Decompile 문자열 만들기 (code 변경 감지)
            int codeHash = HashCodeOf(expr.Code);
            if (st.LastDecompiled == null || st.LastCodeHash != codeHash) {
                st.LastDecompiled = DecompileToInfix(expr.Code);
                st.LastCodeHash   = codeHash;

                // 처음 진입이거나, 외부에서 Expression이 바뀐 경우 편집 텍스트를 최신으로 동기화
                if (string.IsNullOrEmpty(st.EditText) || st.EditText == null || st.EditText == st.LastDecompiled)
                    st.EditText = st.LastDecompiled;

                // code가 바뀌면 에러는 초기화
                st.LastError = null;
            }

            bool modified = st.EditText != st.LastDecompiled;

            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.BeginBoxHeader();
            GUILayout.Label(label ?? GUIContent.none);

            GUILayout.FlexibleSpace();

            // 정보
            GUILayout.Label($"Ops: {expr.Code?.Length ?? 0}  Stack: {expr.MaxStack}", EditorStyles.miniLabel);

            // 버튼들
            if (GUILayout.Button("Revert", EditorStyles.miniButton, GUILayout.Width(70))) {
                st.EditText  = st.LastDecompiled;
                st.LastError = null;
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Compile", EditorStyles.miniButton, GUILayout.Width(70))) {
                TryCompileAndAssign(st, entry, prop);
                // 성공하면 st.LastDecompiled 갱신은 다음 GUI 프레임에서 codeHash 변경으로 자동 갱신됨
            }

            SirenixEditorGUI.EndBoxHeader();

            // 편집 영역
            EditorGUI.BeginChangeCheck();
            st.EditText = SirenixEditorFields.TextField(st.EditText, GUILayout.MinHeight(54));
            if (EditorGUI.EndChangeCheck()) {
                // 단순 변경 표시만 하고 자동 컴파일은 안 함(요구사항: Compile 버튼)
                st.LastError = null;
            }

            // 상태/경고 표시
            if (modified) {
                SirenixEditorGUI.MessageBox("Modified (not compiled yet).", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(st.LastError)) {
                SirenixEditorGUI.MessageBox(st.LastError, MessageType.Error);
            }

            SirenixEditorGUI.DrawThickHorizontalSeparator();

            // 접기/펼치기: Bytecode
            st.ShowDisasm = SirenixEditorGUI.Foldout(st.ShowDisasm, "Bytecode (RPN)");
            if (st.ShowDisasm) {
                string rpn = Disasm(expr.Code);
                SirenixEditorFields.TextField(rpn, GUILayout.MinHeight(90));
            }

            SirenixEditorGUI.EndBox();
        }

        private static void TryCompileAndAssign(State st, IPropertyValueEntry<global::Expression.Expression> entry, InspectorProperty prop) {
            try {
                // 여기서만 string을 사용(사용자가 편집했으니까 당연히 필요)
                var newExpr = new global::Expression.Expression(st.EditText);

                entry.SmartValue = newExpr;

                // 성공 시 에러 제거 + edit을 최신 역표시로 맞춰주기
                st.LastError      = null;
                st.LastDecompiled = DecompileToInfix(newExpr.Code);
                st.LastCodeHash   = HashCodeOf(newExpr.Code);
                st.EditText       = st.LastDecompiled;

                MarkDirty(prop);
                GUI.FocusControl(null);
            }
            catch (Exception e) {
                st.LastError = $"Compile failed: {e.Message}";
            }
        }

        private static void MarkDirty(InspectorProperty prop) {
            var unityObj = prop.Tree?.UnitySerializedObject?.targetObject as UnityEngine.Object;
            if (unityObj != null)
                EditorUtility.SetDirty(unityObj);
        }

        private static int MakeStateKey(InspectorProperty prop) {
            // targetObject + property path 조합으로 안정적인 키
            var target = prop.Tree?.UnitySerializedObject?.targetObject as UnityEngine.Object;
            int id     = target != null ? target.GetInstanceID() : 0;
            unchecked {
                return (id * 397) ^ (prop.Path?.GetHashCode() ?? 0);
            }
        }

        private static int HashCodeOf(global::Expression.ExpressionValue[] code) {
            if (code == null) return 0;
            unchecked {
                int h = 17;
                h = h * 31 + code.Length;
                // 너무 길면 일부만 섞어도 충분
                int step = Math.Max(1, code.Length / 16);
                for (int i = 0; i < code.Length; i += step) {
                    h = h * 31 + (int)code[i].Op;
                    h = h * 31 + code[i].Key;
                    h = h * 31 + code[i].Arity;
                    h = h * 31 + (int)code[i].Func;
                    // Const는 해시 비용 크니 생략(원하면 BitConverter.DoubleToInt64Bits로 추가 가능)
                }

                return h;
            }
        }

        // =========================
        // Decompile (RPN -> Infix)
        // =========================

        private readonly struct Node {
            public readonly string Text;
            public readonly int    Prec;

            public Node(string text, int prec) {
                Text = text;
                Prec = prec;
            }
        }

        // precedence: atom(4) > unary(3) > mul/div(2) > add/sub(1)
        private const int PREC_ADD   = 1;
        private const int PREC_MUL   = 2;
        private const int PREC_UNARY = 3;
        private const int PREC_ATOM  = 4;

        private static string DecompileToInfix(global::Expression.ExpressionValue[] code) {
            if (code == null || code.Length == 0) return string.Empty;

            var stack = new List<Node>(Math.Max(8, code.Length));

            for (int i = 0; i < code.Length; i++) {
                var ins = code[i];

                switch (ins.Op) {
                    case global::Expression.Operator.PushConst:
                        stack.Add(new Node(FormatConst(ins.Const), PREC_ATOM));
                        break;

                    case global::Expression.Operator.PushVar:
                        stack.Add(new Node(FormatVar(ins.Key), PREC_ATOM));
                        break;

                    case global::Expression.Operator.Add:
                        Bin(stack, "+", PREC_ADD, needsRightParenWhenEqualPrec: false);
                        break;

                    case global::Expression.Operator.Sub:
                        // a - (b + c) 같은 경우 괄호 필요: rightPrec <= PREC_ADD 이면 괄호
                        Bin(stack, "-", PREC_ADD, needsRightParenWhenEqualPrec: true);
                        break;

                    case global::Expression.Operator.Mul:
                        Bin(stack, "*", PREC_MUL, needsRightParenWhenEqualPrec: false);
                        break;

                    case global::Expression.Operator.Div:
                        // a / (b * c) 같은 경우 괄호 필요: rightPrec <= PREC_MUL 이면 괄호
                        Bin(stack, "/", PREC_MUL, needsRightParenWhenEqualPrec: true);
                        break;

                    case global::Expression.Operator.Neg:
                        if (stack.Count < 1) throw new FormatException("Decompile stack underflow (neg).");
                    {
                        var    a     = Pop(stack);
                        string inner = (a.Prec < PREC_UNARY) ? $"({a.Text})" : a.Text;
                        stack.Add(new Node("-" + inner, PREC_UNARY));
                    }
                        break;

                    case global::Expression.Operator.CallFunc:
                        Call(stack, ins.Func, ins.Arity);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown opcode: {ins.Op}");
                }
            }

            if (stack.Count != 1) throw new FormatException("Decompile failed (stack not balanced).");
            return stack[0].Text;
        }

        private static void Bin(List<Node> st, string op, int prec, bool needsRightParenWhenEqualPrec) {
            if (st.Count < 2) throw new FormatException("Decompile stack underflow (bin).");

            var b = Pop(st);
            var a = Pop(st);

            string left = (a.Prec < prec) ? $"({a.Text})" : a.Text;

            bool rightNeedParen =
                (b.Prec < prec) || (needsRightParenWhenEqualPrec && b.Prec == prec);

            string right = rightNeedParen ? $"({b.Text})" : b.Text;

            st.Add(new Node($"{left} {op} {right}", prec));
        }

        private static void Call(List<Node> st, global::Expression.ExprFunction func, byte arity) {
            int n = arity;
            if (n <= 0) throw new FormatException("Decompile invalid function arity.");
            if (st.Count < n) throw new FormatException("Decompile stack underflow (call).");

            // RPN에서 마지막 n개가 인자들(뒤가 top) → pop 후 역순으로 배열
            var args = new string[n];
            for (int i = n - 1; i >= 0; i--) {
                args[i] = Pop(st).Text;
            }

            string fn   = func.ToString(); // Pow/Log/Min/Max
            string text = $"{fn}({string.Join(", ", args)})";
            st.Add(new Node(text, PREC_ATOM));
        }

        private static Node Pop(List<Node> st) {
            int idx = st.Count - 1;
            var v   = st[idx];
            st.RemoveAt(idx);
            return v;
        }

        private static string FormatConst(double v) {
            return v.ToString("G17", CultureInfo.InvariantCulture);
        }

        private static string FormatVar(int key) {
            string name = global::Expression.ValueStringKey.GetName(key);
            if (string.IsNullOrEmpty(name))
                return $"Key#{key}";
            return name;
        }

        private static string Disasm(global::Expression.ExpressionValue[] code) {
            if (code == null || code.Length == 0) return "(empty)";

            var sb = new StringBuilder(code.Length * 24);

            for (int i = 0; i < code.Length; i++) {
                ref readonly var ins = ref code[i];
                sb.Append(i.ToString("D3")).Append(": ");

                switch (ins.Op) {
                    case global::Expression.Operator.PushConst:
                        sb.Append("PUSH_CONST ").Append(FormatConst(ins.Const));
                        break;
                    case global::Expression.Operator.PushVar:
                        sb.Append("PUSH_VAR ").Append(FormatVar(ins.Key));
                        break;

                    case global::Expression.Operator.Add: sb.Append("ADD"); break;
                    case global::Expression.Operator.Sub: sb.Append("SUB"); break;
                    case global::Expression.Operator.Mul: sb.Append("MUL"); break;
                    case global::Expression.Operator.Div: sb.Append("DIV"); break;
                    case global::Expression.Operator.Neg: sb.Append("NEG"); break;

                    case global::Expression.Operator.CallFunc:
                        sb.Append("CALL ").Append(ins.Func).Append(" arity=").Append(ins.Arity);
                        break;

                    default:
                        sb.Append("UNKNOWN ").Append(ins.Op);
                        break;
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}
#endif