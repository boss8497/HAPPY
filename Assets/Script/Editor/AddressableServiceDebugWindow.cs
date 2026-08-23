using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Script.Addressable;
using Script.LifetimeScope;
using UnityEditor;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;

namespace Script.Editor {
    /// <summary>
    /// AddressableService의 중앙 캐시(_cache, RefCount+유예시간)는 전부 private — 프로덕션 코드에 디버그용
    /// public 접근자를 추가하는 대신 읽기 전용 리플렉션으로 들여다본다. AddressableService는 MonoBehaviour가
    /// 아니라 VContainer App Scope Singleton이라 씬 탐색으로 못 찾고, AppLifetimeScope.Container.Resolve로 얻는다.
    /// </summary>
    public sealed class AddressableServiceDebugWindow : EditorWindow {
        private const BindingFlags InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags EntryPublicInstance = BindingFlags.Public | BindingFlags.Instance;

        private static readonly FieldInfo CacheField          = typeof(AddressableService).GetField("_cache", InstanceNonPublic);
        private static readonly FieldInfo CheckIntervalField  = typeof(AddressableService).GetField("_cacheCheckIntervalSeconds", InstanceNonPublic);
        private static readonly FieldInfo ReleaseGraceField   = typeof(AddressableService).GetField("_cacheReleaseGraceSeconds", InstanceNonPublic);
        private static readonly FieldInfo CacheMonitorCtsField = typeof(AddressableService).GetField("_cacheMonitorCts", InstanceNonPublic);
        private static readonly FieldInfo AppLabelsField      = typeof(AddressableService).GetField("_appLabels", InstanceNonPublic);

        private static readonly Type      CacheEntryType              = typeof(AddressableService).GetNestedType("CacheEntry", InstanceNonPublic);
        private static readonly FieldInfo EntryHandleField             = CacheEntryType?.GetField("Handle", EntryPublicInstance);
        private static readonly FieldInfo EntryRefCountField           = CacheEntryType?.GetField("RefCount", EntryPublicInstance);
        private static readonly FieldInfo EntryReleasePendingSinceField = CacheEntryType?.GetField("ReleasePendingSince", EntryPublicInstance);

        private static readonly Color GreenColor  = new(0.42f, 0.85f, 0.42f);
        private static readonly Color YellowColor = new(0.95f, 0.85f, 0.25f);
        private static readonly Color OrangeColor = new(1f, 0.6f, 0.2f);

        [MenuItem("Tools/Debug/Addressable Service")]
        private static void Open() {
            var window = GetWindow<AddressableServiceDebugWindow>();
            window.titleContent = new GUIContent("Addressable Service");
            window.minSize      = new Vector2(640f, 480f);
            window.Show();
        }

        private AddressableService _service;

        private Vector2 _scroll;
        private string  _search      = string.Empty;
        private bool    _autoRefresh = true;
        private double  _lastAutoRepaint;
        private bool    _foldoutCache = true;

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
                    "Play 모드가 아닙니다 — AddressableService는 VContainer App Scope Singleton이라 Play 모드에서만 관찰할 수 있습니다.",
                    MessageType.Info);
                return;
            }

            _service = ResolveService();
            if (_service == null) {
                EditorGUILayout.HelpBox(
                    "AddressableService를 찾을 수 없습니다 — AppLifetimeScope가 아직 Container를 빌드하기 전입니다.",
                    MessageType.Warning);
                return;
            }

            try {
                DrawServiceState();
                GUILayout.Space(6f);
                DrawCache();
            }
            catch (Exception e) {
                EditorGUILayout.HelpBox(
                    $"AddressableService 내부 구조가 바뀐 것 같습니다(리플렉션 실패): {e.Message}\n" +
                    "필드명이 바뀌었다면 이 파일 상단의 FieldInfo 캐시를 갱신해야 합니다.",
                    MessageType.Error);
            }
        }

        private static AddressableService ResolveService() {
            var appScope = FindFirstObjectByType<AppLifetimeScope>();
            if (appScope == null || appScope.Container == null) return null;
            return appScope.Container.Resolve<IAddressableService>() as AddressableService;
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

        // ── AddressableService 상태 ─────────────────────────────────────

        private void DrawServiceState() {
            EditorGUILayout.LabelField("AddressableService 상태", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            StateChip("IsInitialized", _service.IsInitialized, GreenColor, 120f);

            var monitorCts     = (CancellationTokenSource)CacheMonitorCtsField.GetValue(_service);
            var monitorRunning = monitorCts != null && monitorCts.IsCancellationRequested == false;
            StateChip("캐시 점검 루프", monitorRunning, GreenColor, 120f);
            EditorGUILayout.EndHorizontal();

            var checkInterval = (float)CheckIntervalField.GetValue(_service);
            var releaseGrace  = (float)ReleaseGraceField.GetValue(_service);
            var appLabels     = (string)AppLabelsField.GetValue(_service);
            EditorGUILayout.LabelField(
                $"점검 주기 {checkInterval:F0}초 / 유예시간 {releaseGrace:F0}초  (GameSettingData에서 설정, ConfigureCache로 반영)",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"앱 필수 라벨: {appLabels}", EditorStyles.miniLabel);
        }

        // ── 캐시 엔트리 ────────────────────────────────────────────────

        private void DrawCache() {
            var cacheDict = (IDictionary)CacheField.GetValue(_service);

            var entries = new List<(string key, object entry)>();
            foreach (DictionaryEntry kv in cacheDict) {
                entries.Add(((string)kv.Key, kv.Value));
            }

            var loadingCount = 0;
            var activeCount  = 0;
            var pendingCount = 0;
            foreach (var (_, entry) in entries) {
                var handle = (AsyncOperationHandle)EntryHandleField.GetValue(entry);
                if (handle.IsValid() == false) {
                    loadingCount++;
                    continue;
                }
                var refCount = (int)EntryRefCountField.GetValue(entry);
                if (refCount > 0) activeCount++;
                else pendingCount++;
            }

            _foldoutCache = EditorGUILayout.Foldout(
                _foldoutCache,
                $"캐시 엔트리 ({entries.Count}개 — 로딩 중 {loadingCount} · 사용 중 {activeCount} · 유예 대기 {pendingCount})",
                true);
            if (_foldoutCache == false) return;

            if (entries.Count == 0) {
                EditorGUILayout.LabelField("캐시된 에셋이 없습니다.", EditorStyles.miniLabel);
                return;
            }

            var releaseGrace = (float)ReleaseGraceField.GetValue(_service);
            foreach (var (key, entry) in entries.OrderBy(e => e.key)) {
                DrawEntryRow(key, entry, releaseGrace);
            }
        }

        private void DrawEntryRow(string key, object entry, float releaseGrace) {
            var refCount = (int)EntryRefCountField.GetValue(entry);
            var handle   = (AsyncOperationHandle)EntryHandleField.GetValue(entry);
            var displayName = DisplayName(key, handle);

            if (PassSearch(key) == false && PassSearch(displayName) == false) return;

            EditorGUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            EditorGUILayout.LabelField(new GUIContent(displayName, key), GUILayout.Width(220));
            StateChip($"RefCount {refCount}", refCount > 0, GreenColor, 100f);

            if (handle.IsValid() == false) {
                StateChip("로딩 중", true, YellowColor, 150f);
                EditorGUILayout.LabelField("-", GUILayout.Width(120));
            }
            else if (refCount > 0) {
                StateChip("사용 중 (pin)", true, GreenColor, 150f);
                EditorGUILayout.LabelField(ResultTypeName(handle), GUILayout.Width(120));
            }
            else {
                var pendingSince = (float)EntryReleasePendingSinceField.GetValue(entry);
                var remaining    = Mathf.Max(0f, releaseGrace - (Time.unscaledTime - pendingSince));
                StateChip($"유예 대기 (남은 {remaining:F0}초)", true, OrangeColor, 150f);
                EditorGUILayout.LabelField(ResultTypeName(handle), GUILayout.Width(120));
            }

            if (handle.IsValid() && handle.Result is UnityEngine.Object unityObj) {
                if (GUILayout.Button("선택", GUILayout.Width(44))) {
                    Selection.activeObject = unityObj;
                    EditorGUIUtility.PingObject(unityObj);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static string ResultTypeName(AsyncOperationHandle handle) {
            return handle.Result != null ? handle.Result.GetType().Name : "-";
        }

        /// <summary>
        /// [AssetPath] 기반 key는 사람이 못 읽는 GUID 그 자체다(CharacterInfo.skeletonDataAsset 등).
        /// 로드가 끝났으면 실제 로드된 오브젝트의 이름을 쓰고, 로딩 중이라 아직 Result가 없으면
        /// key를 GUID로 간주해 AssetDatabase로 파일명을 역추적한다 — 둘 다 실패하면 원래 key를 그대로 보여준다.
        /// </summary>
        private static string DisplayName(string key, AsyncOperationHandle handle) {
            if (handle.IsValid() && handle.Result is UnityEngine.Object obj && obj != null) {
                return obj.name;
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
