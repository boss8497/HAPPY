using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Script.GUI.Screen;
using Script.GUI.Screen.Enum;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace Script.Editor {
    /// <summary>
    /// ScreenManager는 캡슐화를 위해 상태(_screens/_loadedScreens/_firstScreen 등)를 대부분 private으로 들고 있다.
    /// 이 창은 디버깅 전용이라 프로덕션 코드에 public 접근자를 추가하는 대신 읽기 전용 리플렉션으로 들여다본다 —
    /// 런타임 동작에는 전혀 관여하지 않으며, ScreenManager 내부 필드명이 바뀌면 아래 섹션이 에러 메시지로 알려준다.
    /// </summary>
    public sealed class ScreenManagerDebugWindow : EditorWindow {
        private const BindingFlags InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly FieldInfo ScreensField        = typeof(ScreenManager).GetField("_screens", InstanceNonPublic);
        private static readonly FieldInfo LoadedScreensField  = typeof(ScreenManager).GetField("_loadedScreens", InstanceNonPublic);
        private static readonly FieldInfo FirstScreenField    = typeof(ScreenManager).GetField("_firstScreen", InstanceNonPublic);
        private static readonly FieldInfo OpenQueueField      = typeof(ScreenManager).GetField("_openWaitQueue", InstanceNonPublic);
        private static readonly FieldInfo CloseQueueField     = typeof(ScreenManager).GetField("_closeWaitQueue", InstanceNonPublic);
        private static readonly FieldInfo LoadingScreenField  = typeof(ScreenManager).GetField("_loadingScreen", InstanceNonPublic);
        private static readonly FieldInfo LoadingShownField   = typeof(ScreenManager).GetField("_loadingScreenShown", InstanceNonPublic);
        private static readonly FieldInfo SafeAreaScreenField = typeof(ScreenManager).GetField("_safeAreaScreen", InstanceNonPublic);
        private static readonly FieldInfo SafeAreaShownField  = typeof(ScreenManager).GetField("_safeAreaScreenShown", InstanceNonPublic);
        private static readonly FieldInfo StageSnapshotField  = typeof(ScreenManager).GetField("_stageTransitionSnapshot", InstanceNonPublic);

        private static readonly Color GreenColor  = new(0.42f, 0.85f, 0.42f);
        private static readonly Color YellowColor = new(0.95f, 0.85f, 0.25f);
        private static readonly Color OrangeColor = new(1f, 0.6f, 0.2f);
        private static readonly Color GrayColor   = new(0.65f, 0.65f, 0.65f);
        private static readonly Color BlueColor   = new(0.4f, 0.75f, 1f);

        [MenuItem("Tools/Debug/Screen Manager")]
        private static void Open() {
            var window = GetWindow<ScreenManagerDebugWindow>();
            window.titleContent = new GUIContent("Screen Manager");
            window.minSize      = new Vector2(760f, 560f);
            window.Show();
        }

        private ScreenManager _manager;
        private ScreenData    _editorScreenData;

        private Vector2 _scroll;
        private string  _search      = string.Empty;
        private bool    _autoRefresh = true;
        private double  _lastAutoRepaint;

        private bool _foldoutStack    = true;
        private bool _foldoutLayers   = true;
        private bool _foldoutLoaded   = true;
        private bool _foldoutRegistry;

        private void OnEnable() {
            EditorApplication.update += OnEditorUpdate;
            TryLoadEditorScreenData();
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

        private void TryLoadEditorScreenData() {
            var guids = AssetDatabase.FindAssets("t:" + nameof(ScreenData));
            if (guids.Length == 0) return;
            _editorScreenData = AssetDatabase.LoadAssetAtPath<ScreenData>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void OnGUI() {
            _manager = FindFirstObjectByType<ScreenManager>();

            DrawToolbar();

            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;

            if (EditorApplication.isPlaying == false) {
                EditorGUILayout.HelpBox(
                    "Play 모드가 아닙니다 — 로드된 Screen/스택/Layer 같은 런타임 상태는 Play 모드에서만 볼 수 있습니다. " +
                    "아래 레지스트리(등록된 전체 Screen 목록)는 지금도 확인 가능합니다.",
                    MessageType.Info);
            }
            else if (_manager == null) {
                EditorGUILayout.HelpBox(
                    "씬에서 ScreenManager를 찾을 수 없습니다 — AppLifetimeScope가 아직 이걸 생성/초기화하기 전입니다.",
                    MessageType.Warning);
            }
            else {
                try {
                    DrawManagerState();
                    GUILayout.Space(6f);
                    DrawOverlayState();
                    GUILayout.Space(6f);
                    DrawScreenStack();
                    GUILayout.Space(6f);
                    DrawLayerView();
                    GUILayout.Space(6f);
                    DrawLoadedScreens();
                    GUILayout.Space(6f);
                }
                catch (Exception e) {
                    EditorGUILayout.HelpBox(
                        $"ScreenManager 내부 구조가 바뀐 것 같습니다(리플렉션 실패): {e.Message}\n" +
                        "필드명이 바뀌었다면 이 파일 상단의 FieldInfo 캐시를 갱신해야 합니다.",
                        MessageType.Error);
                }
            }

            DrawRegistry();
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

        // ── ScreenManager 상태 ─────────────────────────────────────────

        private void DrawManagerState() {
            EditorGUILayout.LabelField("ScreenManager 상태", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            StateChip("Initialized", _manager.Initialized, GreenColor, 100f);
            StateChip("OpeningScreen", _manager.OpeningScreen, YellowColor, 110f);
            StateChip("ClosingScreen", _manager.ClosingScreen, OrangeColor, 110f);
            EditorGUILayout.EndHorizontal();

            var registry = (Dictionary<string, ScreenAsset>)ScreensField.GetValue(_manager);
            var loaded   = (Dictionary<string, IScreen>)LoadedScreensField.GetValue(_manager);
            var openCount = CountOpenScreens();
            EditorGUILayout.LabelField($"등록 {registry.Count}개 · 로드됨 {loaded.Count}개 · 열림 {openCount}개", EditorStyles.miniLabel);

            var openQueue  = (Queue<string>)OpenQueueField.GetValue(_manager);
            var closeQueue = (Queue<string>)CloseQueueField.GetValue(_manager);
            EditorGUILayout.LabelField(
                $"Open 대기열 {openQueue.Count}개" + (openQueue.Count > 0 ? $"  [{string.Join(", ", openQueue)}]" : string.Empty),
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                $"Close 대기열 {closeQueue.Count}개" + (closeQueue.Count > 0 ? $"  [{string.Join(", ", closeQueue)}]" : string.Empty),
                EditorStyles.miniLabel);
        }

        private int CountOpenScreens() {
            var count   = 0;
            var current = (IScreen)FirstScreenField.GetValue(_manager);
            while (current != null) {
                count++;
                current = current.Next;
            }
            return count;
        }

        // ── 특수 오버레이 (Loading / SafeArea / StageTransition) ─────────
        // 이 셋은 OpenAsync/InsertScreen을 거치지 않고 ScreenManager가 직접 관리해서
        // 아래 "열려있는 Screen 스택"(LinkedList) 에는 나타나지 않는다.

        private void DrawOverlayState() {
            EditorGUILayout.LabelField("특수 오버레이 (LinkedList 미포함, ScreenManager가 직접 관리)", EditorStyles.boldLabel);

            var loadingScreen = (IScreen)LoadingScreenField.GetValue(_manager);
            DrawOverlayRow("Loading", loadingScreen, (bool)LoadingShownField.GetValue(_manager));

            var safeAreaScreen = (IScreen)SafeAreaScreenField.GetValue(_manager);
            DrawOverlayRow("SafeArea", safeAreaScreen, (bool)SafeAreaShownField.GetValue(_manager));

            var snapshot = (Texture2D)StageSnapshotField.GetValue(_manager);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("StageTransition", GUILayout.Width(100));
            EditorGUILayout.LabelField(snapshot != null ? $"캡처 중 ({snapshot.width}x{snapshot.height})" : "숨김", GUILayout.Width(160));
            if (snapshot != null) {
                var rect = GUILayoutUtility.GetRect(64f, 36f, GUILayout.Width(64f), GUILayout.Height(36f));
                UnityEngine.GUI.DrawTexture(rect, snapshot, ScaleMode.ScaleToFit);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawOverlayRow(string label, IScreen screen, bool shown) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));
            EditorGUILayout.LabelField(screen != null ? "로드됨" : "미로드", GUILayout.Width(60));
            StateChip(shown ? "표시 중" : "숨김", shown, GreenColor, 70f);

            if (screen != null && GUILayout.Button("선택", GUILayout.Width(44))) {
                Selection.activeGameObject = screen.GameObject;
                EditorGUIUtility.PingObject(screen.GameObject);
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── 열려있는 Screen 스택 (LinkedList) ─────────────────────────────

        private void DrawScreenStack() {
            _foldoutStack = EditorGUILayout.Foldout(_foldoutStack, "열려있는 Screen 스택 (LinkedList 순서, [고정]=DontClose)", true);
            if (_foldoutStack == false) return;

            var first = (IScreen)FirstScreenField.GetValue(_manager);
            if (first == null) {
                EditorGUILayout.LabelField("열려있는 Screen이 없습니다.", EditorStyles.miniLabel);
                return;
            }

            var index   = 0;
            var current = first;
            while (current != null) {
                DrawScreenRow(current, index);
                current = current.Next;
                index++;
            }
        }

        private void DrawScreenRow(IScreen screen, int index) {
            if (PassSearch(screen.Key) == false) return;

            EditorGUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);

            EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(20));
            StateChip(screen.DontClose ? "고정" : string.Empty, screen.DontClose, BlueColor, 34f);
            EditorGUILayout.LabelField(screen.Key, GUILayout.Width(150));
            EditorGUILayout.LabelField(screen.LayerType.ToString(), GUILayout.Width(90));

            StateChip("Opening", screen.OpeningScreen, YellowColor, 58f);
            StateChip("Opened", screen.OpenedScreen, GreenColor, 58f);
            StateChip("Closing", screen.ClosingScreen, OrangeColor, 58f);
            StateChip("Closed", screen.ClosedScreen, GrayColor, 58f);

            EditorGUILayout.LabelField(screen.GameObject.activeSelf ? "Active" : "Inactive", GUILayout.Width(56));

            if (GUILayout.Button("선택", GUILayout.Width(44))) {
                Selection.activeGameObject = screen.GameObject;
                EditorGUIUtility.PingObject(screen.GameObject);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── Layer별 보기 ───────────────────────────────────────────────

        private void DrawLayerView() {
            _foldoutLayers = EditorGUILayout.Foldout(_foldoutLayers, "Layer별 보기", true);
            if (_foldoutLayers == false) return;

            var openScreens = new List<IScreen>();
            var current     = (IScreen)FirstScreenField.GetValue(_manager);
            while (current != null) {
                openScreens.Add(current);
                current = current.Next;
            }

            var loadingScreen  = (IScreen)LoadingScreenField.GetValue(_manager);
            var loadingShown   = (bool)LoadingShownField.GetValue(_manager);
            var safeAreaScreen = (IScreen)SafeAreaScreenField.GetValue(_manager);
            var safeAreaShown  = (bool)SafeAreaShownField.GetValue(_manager);

            for (var i = 0; i < (int)ScreenLayerType.Max; i++) {
                var layerType = (ScreenLayerType)i;
                var inLayer   = openScreens.Where(s => s.LayerType == layerType).ToList();

                string overlayNote = null;
                if (layerType == ScreenLayerType.Loading && loadingScreen != null)
                    overlayNote = $"{loadingScreen.Key}  (오버레이 전용 관리, {(loadingShown ? "표시 중" : "숨김")})";
                else if (layerType == ScreenLayerType.SafeArea && safeAreaScreen != null)
                    overlayNote = $"{safeAreaScreen.Key}  (오버레이 전용 관리, {(safeAreaShown ? "표시 중" : "숨김")})";

                if (inLayer.Count == 0 && overlayNote == null) continue;

                EditorGUILayout.LabelField($"[{i}] {layerType}", EditorStyles.boldLabel);
                foreach (var s in inLayer) {
                    if (PassSearch(s.Key) == false) continue;
                    EditorGUILayout.LabelField($"    • {s.Key}  ({s.State})", EditorStyles.miniLabel);
                }
                if (overlayNote != null)
                    EditorGUILayout.LabelField($"    • {overlayNote}", EditorStyles.miniLabel);
            }
        }

        // ── 로드된 Screen (Addressable Instantiate 완료분) ─────────────────

        private void DrawLoadedScreens() {
            _foldoutLoaded = EditorGUILayout.Foldout(
                _foldoutLoaded, "로드된 Screen (Addressable Instantiate 완료, 씬 이동 전까지 캐시 유지)", true);
            if (_foldoutLoaded == false) return;

            var loaded   = (Dictionary<string, IScreen>)LoadedScreensField.GetValue(_manager);
            var registry = (Dictionary<string, ScreenAsset>)ScreensField.GetValue(_manager);

            var openKeys = new HashSet<string>();
            var current  = (IScreen)FirstScreenField.GetValue(_manager);
            while (current != null) {
                openKeys.Add(current.Key);
                current = current.Next;
            }

            foreach (var pair in loaded.OrderBy(p => p.Key)) {
                if (PassSearch(pair.Key) == false) continue;

                EditorGUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
                EditorGUILayout.LabelField(pair.Key, GUILayout.Width(150));

                var address = registry.TryGetValue(pair.Key, out var asset) ? AssetPathOf(asset.screen) : "?";
                EditorGUILayout.LabelField(address, GUILayout.Width(240));

                var isOpen = openKeys.Contains(pair.Key);
                StateChip(isOpen ? "열림" : "닫힘(캐시)", isOpen, GreenColor, 80f);

                if (GUILayout.Button("선택", GUILayout.Width(44))) {
                    Selection.activeGameObject = pair.Value.GameObject;
                    EditorGUIUtility.PingObject(pair.Value.GameObject);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // ── 등록된 전체 Screen 레지스트리 (ScreenData 애셋, Edit 모드에서도 확인 가능) ──

        private void DrawRegistry() {
            _foldoutRegistry = EditorGUILayout.Foldout(_foldoutRegistry, "등록된 전체 Screen 레지스트리 (ScreenData 애셋)", true);
            if (_foldoutRegistry == false) return;

            if (_editorScreenData == null) {
                EditorGUILayout.HelpBox("ScreenData 애셋을 프로젝트에서 찾지 못했습니다.", MessageType.Warning);
                return;
            }

            HashSet<string> loadedKeys = null;
            if (EditorApplication.isPlaying && _manager != null) {
                var loaded = (Dictionary<string, IScreen>)LoadedScreensField.GetValue(_manager);
                loadedKeys = new HashSet<string>(loaded.Keys);
            }

            foreach (var asset in _editorScreenData.screens.OrderBy(a => a.id)) {
                if (PassSearch(asset.id) == false) continue;

                EditorGUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
                EditorGUILayout.LabelField(asset.id, GUILayout.Width(150));

                var wasEnabled = UnityEngine.GUI.enabled;
                UnityEngine.GUI.enabled = false;
                EditorGUILayout.ObjectField(asset.screen?.editorAsset, typeof(GameObject), false, GUILayout.Width(220));
                UnityEngine.GUI.enabled = wasEnabled;

                if (loadedKeys != null) {
                    var isLoaded = loadedKeys.Contains(asset.id);
                    StateChip(isLoaded ? "로드됨" : "미로드", isLoaded, GreenColor, 70f);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // ── 공용 유틸 ──────────────────────────────────────────────────

        private bool PassSearch(string key) {
            return string.IsNullOrEmpty(_search) || key.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string AssetPathOf(AssetReferenceT<GameObject> reference) {
            if (reference == null || string.IsNullOrEmpty(reference.AssetGUID)) return "-";
            var path = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
            return string.IsNullOrEmpty(path) ? reference.AssetGUID : System.IO.Path.GetFileNameWithoutExtension(path);
        }

        private static void StateChip(string label, bool active, Color color, float width) {
            var old = UnityEngine.GUI.color;
            UnityEngine.GUI.color = active ? color : new Color(1f, 1f, 1f, 0.25f);
            UnityEngine.GUILayout.Label(label, EditorStyles.miniButton, GUILayout.Width(width));
            UnityEngine.GUI.color = old;
        }
    }
}
