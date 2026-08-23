using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Script.Utility.Runtime;
using UnityEditor;
using UnityEngine;

namespace Script.Editor {
    /// <summary>
    /// ClassPool/ListPool은 static class 안의 static Dictionary(Pools)라 VContainer 인스턴스도, 씬 오브젝트도
    /// 필요 없다 — 타입 자체에서 바로 리플렉션으로 읽는다(BindingFlags.Static). 그래서 다른 세 디버그 창과 달리
    /// Play 모드가 아니어도 창이 죽지 않는다(대부분 비어 보이겠지만). 둘 다 순수 C# 오브젝트(POCO)를 풀링하는 거라
    /// Hierarchy/Project에 Ping할 대상이 없어 "선택" 버튼은 없다 — 타입별 수량만 보여주는 순수 관찰용 창.
    /// 필드명은 문자열로 하드코딩하지 않고 ClassPool/ListPool이 노출하는 nameof 기반 FieldName 상수로만 참조한다.
    /// </summary>
    public sealed class ClassListPoolDebugWindow : EditorWindow {
        private const BindingFlags StaticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;

        private static readonly FieldInfo ClassPoolsField = typeof(ClassPool).GetField(ClassPool.PoolsFieldName, StaticNonPublic);
        private static readonly FieldInfo ListPoolsField  = typeof(ListPool).GetField(ListPool.PoolsFieldName, StaticNonPublic);

        private static readonly Color GreenColor = new(0.42f, 0.85f, 0.42f);
        private static readonly Color BlueColor  = new(0.4f, 0.75f, 1f);

        [MenuItem("Tools/Debug/Class & List Pool")]
        private static void Open() {
            var window = GetWindow<ClassListPoolDebugWindow>();
            window.titleContent = new GUIContent("Class & List Pool");
            window.minSize      = new Vector2(560f, 480f);
            window.Show();
        }

        private Vector2 _scroll;
        private string  _search      = string.Empty;
        private bool    _autoRefresh = true;
        private double  _lastAutoRepaint;

        private bool _foldoutClassPool = true;
        private bool _foldoutListPool  = true;

        private void OnEnable() {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable() {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate() {
            if (_autoRefresh == false) return;
            if (EditorApplication.timeSinceStartup - _lastAutoRepaint < 0.3d) return;
            _lastAutoRepaint = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void OnGUI() {
            DrawToolbar();

            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;

            if (EditorApplication.isPlaying == false) {
                EditorGUILayout.HelpBox(
                    "Play 모드가 아닙니다 — ClassPool/ListPool은 static 상태라 지금도 볼 수는 있지만, " +
                    "보통 게임이 실행 중이어야 실제로 채워집니다.",
                    MessageType.Info);
            }

            try {
                DrawClassPool();
                GUILayout.Space(6f);
                DrawListPool();
            }
            catch (Exception e) {
                EditorGUILayout.HelpBox(
                    $"ClassPool/ListPool 내부 구조가 바뀐 것 같습니다(리플렉션 실패): {e.Message}\n" +
                    "필드 리네임은 nameof 상수가 컴파일 에러로 막아주니, 이 에러는 필드/타입 구조 자체가 바뀐 경우일 겁니다.",
                    MessageType.Error);
            }
        }

        // ── 툴바 ────────────────────────────────────────────────────────

        private void DrawToolbar() {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Repaint();

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "자동 새로고침", EditorStyles.toolbarButton, GUILayout.Width(90));

            GUILayout.Space(8f);
            GUILayout.Label("검색", GUILayout.Width(30));
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(160));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // ── ClassPool ──────────────────────────────────────────────────

        private void DrawClassPool() {
            var pools = (Dictionary<Type, HashSet<object>>)ClassPoolsField.GetValue(null);
            var totalIdle = pools.Values.Sum(s => s.Count);

            _foldoutClassPool = EditorGUILayout.Foldout(
                _foldoutClassPool, $"ClassPool — {pools.Count}개 타입 · 대기 중 {totalIdle}개", true);
            if (_foldoutClassPool == false) return;

            if (pools.Count == 0) {
                EditorGUILayout.LabelField("풀링된 타입이 없습니다.", EditorStyles.miniLabel);
                return;
            }

            foreach (var pair in pools.OrderBy(p => p.Key.Name)) {
                DrawClassPoolRow(pair.Key, pair.Value);
            }
        }

        private void DrawClassPoolRow(Type type, HashSet<object> set) {
            if (PassSearch(type.Name) == false) return;

            EditorGUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            EditorGUILayout.LabelField(new GUIContent(type.Name, type.FullName), GUILayout.Width(260));
            StateChip($"대기 중 {set.Count}", set.Count > 0, GreenColor, 100f);

            var hasHooks = typeof(IClassPool).IsAssignableFrom(type);
            if (hasHooks) {
                StateChip("OnRent/OnReturn 훅", true, BlueColor, 140f);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── ListPool ───────────────────────────────────────────────────

        private void DrawListPool() {
            var pools = (Dictionary<Type, Stack<ICollection>>)ListPoolsField.GetValue(null);
            var totalIdle = pools.Values.Sum(s => s.Count);

            _foldoutListPool = EditorGUILayout.Foldout(
                _foldoutListPool, $"ListPool — {pools.Count}개 타입 · 대기 중 {totalIdle}개", true);
            if (_foldoutListPool == false) return;

            if (pools.Count == 0) {
                EditorGUILayout.LabelField("풀링된 타입이 없습니다.", EditorStyles.miniLabel);
                return;
            }

            foreach (var pair in pools.OrderBy(p => p.Key.Name)) {
                DrawListPoolRow(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Pools의 key는 Get&lt;T&gt;/Return&lt;T&gt; 경로면 "요소 타입"(실제 저장물은 List&lt;T&gt;)이고,
        /// GetCollection&lt;T&gt;/ReturnCollection 경로면 "컬렉션 타입 그 자체"라 의미가 다르다.
        /// 그래서 key를 그대로 믿지 않고, 대기 중인 항목이 있으면 실제 저장된 오브젝트의 런타임 타입을 우선한다.
        /// </summary>
        private void DrawListPoolRow(Type key, Stack<ICollection> stack) {
            if (PassSearch(key.Name) == false) return;

            var actualType = stack.Count > 0 ? stack.Peek().GetType() : null;
            var label      = actualType != null ? FriendlyTypeName(actualType) : key.Name;

            EditorGUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            EditorGUILayout.LabelField(new GUIContent(label, key.FullName), GUILayout.Width(260));
            StateChip($"대기 중 {stack.Count}", stack.Count > 0, GreenColor, 100f);
            EditorGUILayout.EndHorizontal();
        }

        private static string FriendlyTypeName(Type type) {
            if (type.IsGenericType == false) return type.Name;

            var backtickIndex = type.Name.IndexOf('`');
            var name = backtickIndex >= 0 ? type.Name[..backtickIndex] : type.Name;
            var args = string.Join(", ", type.GetGenericArguments().Select(FriendlyTypeName));
            return $"{name}<{args}>";
        }

        // ── 공용 유틸 ──────────────────────────────────────────────────

        private bool PassSearch(string key) {
            return string.IsNullOrEmpty(_search) || key.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void StateChip(string label, bool active, Color color, float width) {
            var old = UnityEngine.GUI.color;
            UnityEngine.GUI.color = active ? color : new Color(1f, 1f, 1f, 0.25f);
            GUILayout.Label(label, EditorStyles.miniButton, GUILayout.Width(width));
            UnityEngine.GUI.color = old;
        }
    }
}
