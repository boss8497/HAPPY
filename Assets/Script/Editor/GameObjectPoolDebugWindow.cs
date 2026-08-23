using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Script.GamePlay.Audio;
using Script.GamePlay.Pool;
using Script.LifetimeScope;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace Script.Editor {
    /// <summary>
    /// GameObjectPool(및 이를 여러 개 들고 있는 UIPooling/AudioPooling/StagePooling)의 상태는 전부 private —
    /// 프로덕션 코드에 디버그용 public 접근자를 추가하는 대신 읽기 전용 리플렉션으로 들여다본다.
    /// 세 매니저 모두 MonoBehaviour가 아닌 VContainer Singleton이라 씬 탐색으로 못 찾고,
    /// UIPooling/AudioPooling은 AppLifetimeScope에서, StagePooling은 StageLifetimeScope(게임 씬에만 존재)에서
    /// Container.Resolve로 얻는다.
    /// </summary>
    public sealed class GameObjectPoolDebugWindow : EditorWindow {
        private const BindingFlags InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly FieldInfo UIPoolsField    = typeof(UIPooling).GetField("_objectPools", InstanceNonPublic);
        private static readonly FieldInfo AudioPoolsField = typeof(AudioPooling).GetField("_objectPools", InstanceNonPublic);
        private static readonly FieldInfo StagePoolsField = typeof(StagePooling).GetField("_objectPools", InstanceNonPublic);

        private static readonly FieldInfo PoolPooledField   = typeof(GameObjectPool).GetField("_pooled", InstanceNonPublic);
        private static readonly FieldInfo PoolInstanceField = typeof(GameObjectPool).GetField("_instance", InstanceNonPublic);
        private static readonly FieldInfo PoolDisposedField = typeof(GameObjectPool).GetField("_isDisposed", InstanceNonPublic);

        private static readonly Color GreenColor  = new(0.42f, 0.85f, 0.42f);
        private static readonly Color OrangeColor = new(1f, 0.6f, 0.2f);

        [MenuItem("Tools/Debug/GameObject Pool")]
        private static void Open() {
            var window = GetWindow<GameObjectPoolDebugWindow>();
            window.titleContent = new GUIContent("GameObject Pool");
            window.minSize      = new Vector2(640f, 520f);
            window.Show();
        }

        private Vector2 _scroll;
        private string  _search      = string.Empty;
        private bool    _autoRefresh = true;
        private double  _lastAutoRepaint;

        private bool _foldoutUI    = true;
        private bool _foldoutAudio = true;
        private bool _foldoutStage = true;

        private void OnEnable() {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable() {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate() {
            if (_autoRefresh == false || EditorApplication.isPlaying == false) return;
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
                    "Play 모드가 아닙니다 — Pooling 매니저들은 VContainer Singleton이라 Play 모드에서만 관찰할 수 있습니다.",
                    MessageType.Info);
                return;
            }

            try {
                var appScope = FindFirstObjectByType<AppLifetimeScope>();

                UIPooling uiPooling = null;
                AudioPooling audioPooling = null;
                if (appScope != null && appScope.Container != null) {
                    uiPooling    = appScope.Container.Resolve<IUIPooling>() as UIPooling;
                    audioPooling = appScope.Container.Resolve<IAudioPooling>() as AudioPooling;
                }

                StagePooling stagePooling = null;
                var stageScope = FindFirstObjectByType<StageLifetimeScope>();
                if (stageScope != null && stageScope.Container != null) {
                    stagePooling = stageScope.Container.Resolve<IStagePooling>() as StagePooling;
                }

                DrawManagerSection("UI Pooling", uiPooling, UIPoolsField, ref _foldoutUI,
                    "AppLifetimeScope Container를 아직 빌드하기 전입니다.");

                DrawManagerSection("Audio Pooling", audioPooling, AudioPoolsField, ref _foldoutAudio,
                    "AppLifetimeScope Container를 아직 빌드하기 전입니다.");

                DrawManagerSection("Stage Pooling", stagePooling, StagePoolsField, ref _foldoutStage,
                    "스테이지 씬이 아니거나 StageLifetimeScope가 아직 빌드되기 전입니다.");
            }
            catch (Exception e) {
                EditorGUILayout.HelpBox(
                    $"Pooling 내부 구조가 바뀐 것 같습니다(리플렉션 실패): {e.Message}\n" +
                    "필드명이 바뀌었다면 이 파일 상단의 FieldInfo 캐시를 갱신해야 합니다.",
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

        // ── 매니저 섹션 ────────────────────────────────────────────────

        private void DrawManagerSection(string title, object manager, FieldInfo poolsField, ref bool foldout, string notFoundMessage) {
            if (manager == null) {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(notFoundMessage, MessageType.Info);
                GUILayout.Space(6f);
                return;
            }

            var pools = (Dictionary<string, GameObjectPool>)poolsField.GetValue(manager);
            var idleTotal = 0;
            foreach (var pool in pools.Values) {
                idleTotal += PooledCount(pool);
            }

            foldout = EditorGUILayout.Foldout(foldout, $"{title} — {pools.Count}개 풀 · 대기 중 {idleTotal}개", true);
            if (foldout == false) {
                GUILayout.Space(6f);
                return;
            }

            if (pools.Count == 0) {
                EditorGUILayout.LabelField("생성된 풀이 없습니다.", EditorStyles.miniLabel);
            }
            else {
                foreach (var pair in pools.OrderBy(p => p.Key)) {
                    DrawPoolRow(pair.Key, pair.Value);
                }
            }

            GUILayout.Space(6f);
        }

        private void DrawPoolRow(string key, GameObjectPool pool) {
            var template  = (GameObject)PoolInstanceField.GetValue(pool);
            var idleCount = PooledCount(pool);
            var disposed  = (bool)PoolDisposedField.GetValue(pool);
            var displayName = DisplayName(key, template);

            if (PassSearch(key) == false && PassSearch(displayName) == false) return;

            EditorGUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            EditorGUILayout.LabelField(new GUIContent(displayName, key), GUILayout.Width(220));
            StateChip($"대기 중 {idleCount}", idleCount > 0, GreenColor, 100f);
            EditorGUILayout.LabelField($"BaseScale {pool.BaseScale:F2}", GUILayout.Width(140));

            if (disposed) {
                StateChip("Disposed", true, OrangeColor, 80f);
            }

            if (template != null && GUILayout.Button("선택", GUILayout.Width(44))) {
                Selection.activeObject = template;
                EditorGUIUtility.PingObject(template);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static int PooledCount(GameObjectPool pool) {
            var pooled = (HashSet<GameObject>)PoolPooledField.GetValue(pool);
            return pooled.Count;
        }

        /// <summary>
        /// Pop/Push에 쓰이는 key는 [AssetPath] 기반 GUID인 경우가 많아 사람이 못 읽는다.
        /// 풀의 템플릿 인스턴스(_instance)가 살아있으면 그 오브젝트 이름("(Clone)" 접미사 제거)을 쓰고,
        /// 없으면(디스포즈됨 등) key를 GUID로 간주해 AssetDatabase로 파일명을 역추적한다.
        /// </summary>
        private static string DisplayName(string key, GameObject template) {
            if (template != null) {
                const string cloneSuffix = "(Clone)";
                var name = template.name;
                return name.EndsWith(cloneSuffix) ? name[..^cloneSuffix.Length] : name;
            }

            var path = AssetDatabase.GUIDToAssetPath(key);
            return string.IsNullOrEmpty(path) ? key : System.IO.Path.GetFileNameWithoutExtension(path);
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
