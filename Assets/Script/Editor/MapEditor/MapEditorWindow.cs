#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Script.GameInfo.Character;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.Editor.MapEditor {
    public sealed class MapEditorWindow : OdinEditorWindow {
        // ── 레이아웃 ────────────────────────────────────────────────────
        private const float LeftWidth    = 300f;
        private const float PaletteThumb = 64f;

        private Vector2 _leftScroll;
        private Vector2 _paletteScroll;
        private Vector2 _objectPaletteScroll;
        private Vector2 _dataScroll;

        // ── 팔레트 필터 ─────────────────────────────────────────────────
        private string _paletteFilter = string.Empty;
        private string _objectFilter  = string.Empty;

        // ── 메뉴 ────────────────────────────────────────────────────────
        [MenuItem("Tools/Map Editor")]
        private static void OpenWindow() {
            var window = GetWindow<MapEditorWindow>();
            window.titleContent = new GUIContent("Map Editor");
            window.minSize      = new Vector2(900f, 600f);
            window.Show();
        }

        protected override void OnEnable() {
            base.OnEnable();
            MapEditorSceneGUI.Register();
            TryLoadTables();
            RefreshPalette();
            RefreshObjectPalette();
        }

        protected override void OnDisable() {
            base.OnDisable();
            MapEditorSceneGUI.Unregister();
        }

        protected override void OnImGUI() {
            base.OnImGUI();
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        // ── 툴바 ────────────────────────────────────────────────────────

        private void DrawToolbar() {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Reload Tables", EditorStyles.toolbarButton, GUILayout.Width(100))) {
                TryLoadTables();
                RefreshPalette();
                RefreshObjectPalette();
            }

            GUILayout.Space(8f);

            var hasDirty = MapEditorState.IsDirty;
            var oldColor = UnityEngine.GUI.color;
            if (hasDirty) UnityEngine.GUI.color = new Color(1f, 0.85f, 0.3f);

            var hasAnyAction = MapEditorState.CurrentMapAction != null || MapEditorState.CurrentEnemySpawnAction != null;

            UnityEngine.GUI.enabled = hasAnyAction;
            if (GUILayout.Button("Save to PhaseInfo", EditorStyles.toolbarButton, GUILayout.Width(130))) {
                SaveToPhaseInfo();
            }
            UnityEngine.GUI.enabled = true;
            UnityEngine.GUI.color   = oldColor;

            UnityEngine.GUI.enabled = hasAnyAction;
            if (GUILayout.Button("Reload from PhaseInfo", EditorStyles.toolbarButton, GUILayout.Width(150))) {
                MapEditorState.LoadFromMapAction();
                SceneView.RepaintAll();
            }
            UnityEngine.GUI.enabled = true;

            GUILayout.Space(16f);

            DrawModeButtons();

            GUILayout.FlexibleSpace();

            if (hasDirty)
                GUILayout.Label("● 미저장 변경 사항", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawModeButtons() {
            var modes  = new[] { MapEditMode.Select, MapEditMode.TilePlacement, MapEditMode.ObjectPlacement, MapEditMode.HeightPoint };
            var labels = new[] { "Select", "Tile 배치", "Object 배치", "HeightPoint" };

            for (var i = 0; i < modes.Length; i++) {
                var isActive = MapEditorState.EditMode == modes[i];
                var oldBg    = UnityEngine.GUI.backgroundColor;
                if (isActive) UnityEngine.GUI.backgroundColor = new Color(0.4f, 0.8f, 1f, 1f);
                if (GUILayout.Button(labels[i], EditorStyles.toolbarButton, GUILayout.Width(80))) {
                    MapEditorState.EditMode = modes[i];
                    SceneView.RepaintAll();
                }
                UnityEngine.GUI.backgroundColor = oldBg;
            }
        }

        // ── 좌측 패널 ────────────────────────────────────────────────────

        private void DrawLeftPanel() {
            EditorGUILayout.BeginVertical(GUILayout.Width(LeftWidth));
            using (var scroll = new EditorGUILayout.ScrollViewScope(_leftScroll)) {
                _leftScroll = scroll.scrollPosition;
                DrawDataSelector();
                GUILayout.Space(6f);
                DrawTilePalette();
                GUILayout.Space(6f);
                DrawObjectPalette();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawDataSelector() {
            SirenixEditorGUI.BeginBox("맵 데이터 선택");

            if (MapEditorState.DungeonTable == null) {
                SirenixEditorGUI.MessageBox("DungeonTable을 찾을 수 없습니다.", MessageType.Warning);
                SirenixEditorGUI.EndBox();
                return;
            }

            var dungeons = MapEditorState.DungeonTable.DungeonInfos;

            var dungeonNames  = dungeons.Select(d => $"[{d.UID}] {d.category}").ToArray();
            EditorGUILayout.LabelField("Dungeon", EditorStyles.boldLabel);
            var newDungeonIdx = EditorGUILayout.Popup(MapEditorState.SelectedDungeonIndex, dungeonNames);
            if (newDungeonIdx != MapEditorState.SelectedDungeonIndex) {
                MapEditorState.SelectedDungeonIndex     = newDungeonIdx;
                MapEditorState.SelectedStageIndex       = -1;
                MapEditorState.SelectedPhaseUid         = -1;
                MapEditorState.CurrentStage             = null;
                MapEditorState.CurrentPhase             = null;
                MapEditorState.CurrentMapAction         = null;
                MapEditorState.CurrentEnemySpawnAction  = null;
            }

            if (MapEditorState.SelectedDungeonIndex < 0 || MapEditorState.SelectedDungeonIndex >= dungeons.Length) {
                SirenixEditorGUI.EndBox();
                return;
            }

            MapEditorState.CurrentDungeon = dungeons[MapEditorState.SelectedDungeonIndex];

            var stages     = MapEditorState.CurrentDungeon.stages ?? System.Array.Empty<Stage>();
            var stageNames = stages.Select((s, i) => $"[{i}] {s.id}").ToArray();
            GUILayout.Space(4f);
            EditorGUILayout.LabelField("Stage", EditorStyles.boldLabel);
            var newStageIdx = EditorGUILayout.Popup(MapEditorState.SelectedStageIndex, stageNames);
            if (newStageIdx != MapEditorState.SelectedStageIndex) {
                MapEditorState.SelectedStageIndex      = newStageIdx;
                MapEditorState.SelectedPhaseUid        = -1;
                MapEditorState.CurrentPhase            = null;
                MapEditorState.CurrentMapAction        = null;
                MapEditorState.CurrentEnemySpawnAction = null;
            }

            if (MapEditorState.SelectedStageIndex < 0 || MapEditorState.SelectedStageIndex >= stages.Length) {
                SirenixEditorGUI.EndBox();
                return;
            }

            MapEditorState.CurrentStage = stages[MapEditorState.SelectedStageIndex];

            if (MapEditorState.PhaseTable == null) {
                SirenixEditorGUI.MessageBox("PhaseTable을 찾을 수 없습니다.", MessageType.Warning);
                SirenixEditorGUI.EndBox();
                return;
            }

            var phaseUids  = MapEditorState.CurrentStage.phaseInfos ?? System.Array.Empty<int>();
            var phaseNames = phaseUids.Select(uid => {
                var p = MapEditorState.PhaseTable.PhaseInfos.FirstOrDefault(x => x.UID == uid);
                return p != null ? $"[UID:{uid}]" : $"[UID:{uid}] (없음)";
            }).ToArray();

            GUILayout.Space(4f);
            EditorGUILayout.LabelField("Phase", EditorStyles.boldLabel);

            var currentPhaseIdxInStage = System.Array.IndexOf(phaseUids, MapEditorState.SelectedPhaseUid);
            var newPhaseIdxInStage     = EditorGUILayout.Popup(currentPhaseIdxInStage, phaseNames);
            if (newPhaseIdxInStage != currentPhaseIdxInStage || MapEditorState.CurrentPhase == null) {
                if (newPhaseIdxInStage >= 0 && newPhaseIdxInStage < phaseUids.Length) {
                    MapEditorState.SelectedPhaseUid = phaseUids[newPhaseIdxInStage];
                    MapEditorState.CurrentPhase     = MapEditorState.PhaseTable.PhaseInfos
                        .FirstOrDefault(x => x.UID == MapEditorState.SelectedPhaseUid);
                    RefreshActions();
                }
            }

            GUILayout.Space(8f);

            // Load / Clear 버튼
            EditorGUILayout.BeginHorizontal();
            UnityEngine.GUI.enabled = MapEditorState.CurrentMapAction != null || MapEditorState.CurrentEnemySpawnAction != null;
            if (GUILayout.Button("Load", GUILayout.Height(26))) {
                MapEditorState.LoadFromMapAction();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Clear Working Data", GUILayout.Height(26))) {
                if (EditorUtility.DisplayDialog("확인", "작업 중인 타일/HeightPoint/오브젝트 데이터를 초기화합니까?", "초기화", "취소")) {
                    MapEditorState.WorkingTiles.Clear();
                    MapEditorState.WorkingHeightPoints.Clear();
                    MapEditorState.WorkingObjects.Clear();
                    MapEditorState.SelectedObjectIndex = -1;
                    MapEditorState.IsDirty             = true;
                    SceneView.RepaintAll();
                }
            }
            UnityEngine.GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // MapSpawnAction 상태
            if (MapEditorState.CurrentMapAction == null)
                SirenixEditorGUI.MessageBox("선택한 Phase에 MapSpawnAction이 없습니다.", MessageType.Info);

            if (MapEditorState.CurrentPhase != null && MapEditorState.CurrentMapAction == null) {
                if (GUILayout.Button("MapSpawnAction 추가", GUILayout.Height(26))) {
                    AddMapSpawnAction();
                }
            }

            GUILayout.Space(4f);

            // EnemySpawnAction 상태
            if (MapEditorState.CurrentEnemySpawnAction == null)
                SirenixEditorGUI.MessageBox("선택한 Phase에 EnemySpawnAction이 없습니다.", MessageType.Info);

            if (MapEditorState.CurrentPhase != null && MapEditorState.CurrentEnemySpawnAction == null) {
                if (GUILayout.Button("EnemySpawnAction 추가", GUILayout.Height(26))) {
                    AddEnemySpawnAction();
                }
            }

            SirenixEditorGUI.EndBox();
        }

        private void DrawTilePalette() {
            SirenixEditorGUI.BeginBox("Tile Palette  (Assets/GAME_ASSET/Prefab/Tile)");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("검색", GUILayout.Width(36f));
            _paletteFilter = EditorGUILayout.TextField(_paletteFilter);
            if (GUILayout.Button("↺", GUILayout.Width(24f))) RefreshPalette();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4f);

            var entries  = MapEditorState.PaletteEntries;
            var filtered = string.IsNullOrEmpty(_paletteFilter)
                ? entries
                : entries.Where(e => e.Address.ToLower().Contains(_paletteFilter.ToLower())).ToList();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_paletteScroll, GUILayout.MaxHeight(260f))) {
                _paletteScroll = scroll.scrollPosition;

                var col  = 0;
                var cols = Mathf.Max(1, Mathf.FloorToInt((LeftWidth - 24f) / (PaletteThumb + 8f)));
                EditorGUILayout.BeginHorizontal();

                foreach (var entry in filtered) {
                    var isSelected = entry.AddressableGuid == MapEditorState.SelectedPrefabKey;

                    var oldBg = UnityEngine.GUI.backgroundColor;
                    UnityEngine.GUI.backgroundColor = isSelected ? new Color(0.4f, 0.85f, 1f) : Color.white;

                    EditorGUILayout.BeginVertical(GUILayout.Width(PaletteThumb + 4f));
                    var rect = GUILayoutUtility.GetRect(PaletteThumb, PaletteThumb);

                    if (entry.Preview != null)
                        UnityEngine.GUI.DrawTexture(rect, entry.Preview, ScaleMode.ScaleToFit);
                    else
                        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f));

                    if (UnityEngine.GUI.Button(rect, GUIContent.none, GUIStyle.none)) {
                        MapEditorState.SelectedPrefabKey = entry.AddressableGuid;
                        MapEditorState.EditMode          = MapEditMode.TilePlacement;
                    }

                    var nameStyle = new GUIStyle(EditorStyles.miniLabel) {
                        wordWrap  = false,
                        alignment = TextAnchor.UpperCenter,
                        clipping  = TextClipping.Clip,
                    };
                    EditorGUILayout.LabelField(entry.Address, nameStyle, GUILayout.Width(PaletteThumb + 4f));
                    EditorGUILayout.EndVertical();

                    UnityEngine.GUI.backgroundColor = oldBg;

                    col++;
                    if (col >= cols) {
                        col = 0;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (filtered.Count == 0)
                    GUILayout.Label("Assets/GAME_ASSET/Prefab/Tile 에 Addressable 프리팹이 없습니다.", EditorStyles.centeredGreyMiniLabel);
            }

            if (!string.IsNullOrEmpty(MapEditorState.SelectedPrefabKey)) {
                var sel = entries.FirstOrDefault(e => e.AddressableGuid == MapEditorState.SelectedPrefabKey);
                GUILayout.Space(4f);
                EditorGUILayout.LabelField($"선택: {sel.Address ?? MapEditorState.SelectedPrefabKey}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Scene 클릭으로 타일 배치", EditorStyles.miniLabel);
            }

            SirenixEditorGUI.EndBox();
        }

        private void DrawObjectPalette() {
            SirenixEditorGUI.BeginBox("Object Palette  (Obstacle / Buff / Score / Heart / Goal)");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("검색", GUILayout.Width(36f));
            _objectFilter = EditorGUILayout.TextField(_objectFilter);
            if (GUILayout.Button("↺", GUILayout.Width(24f))) RefreshObjectPalette();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4f);

            var entries  = MapEditorState.ObjectPaletteEntries;
            var filtered = string.IsNullOrEmpty(_objectFilter)
                ? entries
                : entries.Where(e => e.Label.ToLower().Contains(_objectFilter.ToLower())).ToList();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_objectPaletteScroll, GUILayout.MaxHeight(240f))) {
                _objectPaletteScroll = scroll.scrollPosition;

                var col  = 0;
                var cols = Mathf.Max(1, Mathf.FloorToInt((LeftWidth - 24f) / (PaletteThumb + 8f)));
                EditorGUILayout.BeginHorizontal();

                foreach (var entry in filtered) {
                    var isSelected = entry.Uid == MapEditorState.SelectedObjectUid;

                    var oldBg = UnityEngine.GUI.backgroundColor;
                    UnityEngine.GUI.backgroundColor = isSelected ? new Color(0.4f, 0.85f, 1f) : Color.white;

                    EditorGUILayout.BeginVertical(GUILayout.Width(PaletteThumb + 4f));
                    var rect = GUILayoutUtility.GetRect(PaletteThumb, PaletteThumb);

                    if (entry.Preview != null)
                        UnityEngine.GUI.DrawTexture(rect, entry.Preview, ScaleMode.ScaleToFit);
                    else
                        EditorGUI.DrawRect(rect, GetTypeColor(entry.CharacterType) * 0.6f);

                    if (UnityEngine.GUI.Button(rect, GUIContent.none, GUIStyle.none)) {
                        MapEditorState.SelectedObjectUid = entry.Uid;
                        MapEditorState.EditMode          = MapEditMode.ObjectPlacement;
                    }

                    var nameStyle = new GUIStyle(EditorStyles.miniLabel) {
                        wordWrap  = false,
                        alignment = TextAnchor.UpperCenter,
                        clipping  = TextClipping.Clip,
                    };
                    EditorGUILayout.LabelField(entry.Label, nameStyle, GUILayout.Width(PaletteThumb + 4f));
                    EditorGUILayout.EndVertical();

                    UnityEngine.GUI.backgroundColor = oldBg;

                    col++;
                    if (col >= cols) {
                        col = 0;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (filtered.Count == 0)
                    GUILayout.Label("Object 타입의 CharacterInfo가 없습니다.", EditorStyles.centeredGreyMiniLabel);
            }

            if (MapEditorState.SelectedObjectUid != 0) {
                var sel = entries.FirstOrDefault(e => e.Uid == MapEditorState.SelectedObjectUid);
                GUILayout.Space(4f);
                EditorGUILayout.LabelField($"선택: {sel.Label}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Scene 클릭으로 오브젝트 배치", EditorStyles.miniLabel);
            }

            SirenixEditorGUI.EndBox();
        }

        // ── 우측 패널 ────────────────────────────────────────────────────

        private void DrawRightPanel() {
            EditorGUILayout.BeginVertical();

            SirenixEditorGUI.BeginBox("조작 안내");
            DrawHelp();
            SirenixEditorGUI.EndBox();

            GUILayout.Space(4f);

            using (var scroll = new EditorGUILayout.ScrollViewScope(_dataScroll)) {
                _dataScroll = scroll.scrollPosition;
                DrawTileList();
                GUILayout.Space(8f);
                DrawObjectList();
                GUILayout.Space(8f);
                DrawHeightPointList();
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawHelp() {
            switch (MapEditorState.EditMode) {
                case MapEditMode.Select:
                    EditorGUILayout.LabelField("● Select 모드: 씬에서 타일/오브젝트/HeightPoint 클릭 → 선택  |  Delete → 삭제", EditorStyles.miniLabel);
                    break;
                case MapEditMode.TilePlacement:
                    EditorGUILayout.LabelField("● Tile 배치: 팔레트에서 프리팹 선택 → 씬 클릭으로 배치  |  Delete → 선택 타일 삭제", EditorStyles.miniLabel);
                    break;
                case MapEditMode.ObjectPlacement:
                    EditorGUILayout.LabelField("● Object 배치: 팔레트에서 오브젝트 선택 → 씬 클릭으로 배치  |  Delete → 선택 오브젝트 삭제", EditorStyles.miniLabel);
                    break;
                case MapEditMode.HeightPoint:
                    EditorGUILayout.LabelField("● HeightPoint: Shift+클릭 → 추가  |  핸들 드래그 → 이동  |  Delete → 삭제  |  X 순서 자동 정렬", EditorStyles.miniLabel);
                    break;
            }
        }

        private void DrawTileList() {
            SirenixEditorGUI.BeginBox($"Tiles  ({MapEditorState.WorkingTiles.Count})");

            var tiles = MapEditorState.WorkingTiles;
            for (var i = 0; i < tiles.Count; i++) {
                var tile       = tiles[i];
                var isSelected = i == MapEditorState.SelectedTileIndex;

                var oldBg = UnityEngine.GUI.backgroundColor;
                if (isSelected) UnityEngine.GUI.backgroundColor = new Color(0.35f, 0.65f, 1f, 0.35f);
                SirenixEditorGUI.BeginBox();

                EditorGUILayout.BeginHorizontal();

                var label = $"[{i}]  pos: ({tile.position.x:F1}, {tile.position.y:F1})";
                if (GUILayout.Button(label, EditorStyles.label, GUILayout.ExpandWidth(true))) {
                    MapEditorState.SelectedTileIndex   = i;
                    MapEditorState.SelectedHeightIndex = -1;
                    MapEditorState.SelectedObjectIndex = -1;
                    FocusSceneOnTile(tile.position);
                }

                if (GUILayout.Button("✕", GUILayout.Width(24f))) {
                    tiles.RemoveAt(i);
                    if (MapEditorState.SelectedTileIndex >= tiles.Count)
                        MapEditorState.SelectedTileIndex = -1;
                    MapEditorState.IsDirty = true;
                    SceneView.RepaintAll();
                    SirenixEditorGUI.EndBox();
                    EditorGUILayout.EndHorizontal();
                    UnityEngine.GUI.backgroundColor = oldBg;
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (isSelected) {
                    EditorGUI.BeginChangeCheck();
                    var newPos = EditorGUILayout.Vector3Field("Position", tile.position);
                    if (EditorGUI.EndChangeCheck()) {
                        tile.position = newPos;
                        tiles[i]      = tile;
                        MapEditorState.IsDirty = true;
                        SceneView.RepaintAll();
                    }

                    var assetPath = AssetDatabase.GUIDToAssetPath(tile.prefabKey);
                    var go        = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    EditorGUI.BeginChangeCheck();
                    var newGo = (GameObject)EditorGUILayout.ObjectField("Prefab", go, typeof(GameObject), false);
                    if (EditorGUI.EndChangeCheck() && newGo != null) {
                        var newPath    = AssetDatabase.GetAssetPath(newGo);
                        var newGuid    = AssetDatabase.AssetPathToGUID(newPath);
                        tile.prefabKey = newGuid;
                        tiles[i]       = tile;
                        MapEditorState.IsDirty = true;
                    }
                }

                SirenixEditorGUI.EndBox();
                UnityEngine.GUI.backgroundColor = oldBg;
            }

            GUILayout.Space(4f);
            if (GUILayout.Button("+ Tile 추가 (현재 선택 프리팹)", GUILayout.Height(26f))) {
                tiles.Add(new MapTileData {
                    position  = GetSceneCenter(),
                    prefabKey = MapEditorState.SelectedPrefabKey
                });
                MapEditorState.SelectedTileIndex = tiles.Count - 1;
                MapEditorState.IsDirty           = true;
                SceneView.RepaintAll();
            }

            SirenixEditorGUI.EndBox();
        }

        private void DrawObjectList() {
            SirenixEditorGUI.BeginBox($"Objects  ({MapEditorState.WorkingObjects.Count})");

            var objects = MapEditorState.WorkingObjects;
            for (var i = 0; i < objects.Count; i++) {
                var obj        = objects[i];
                var isSelected = i == MapEditorState.SelectedObjectIndex;

                var charInfo = MapEditorState.CharacterTable?.CharacterInfos
                    .FirstOrDefault(c => c.UID == obj.uid);

                var oldBg = UnityEngine.GUI.backgroundColor;
                if (isSelected) UnityEngine.GUI.backgroundColor = new Color(0.35f, 1f, 0.5f, 0.35f);
                SirenixEditorGUI.BeginBox();

                EditorGUILayout.BeginHorizontal();

                var label = charInfo != null
                    ? $"[{i}]  [{charInfo.type}] uid:{obj.uid}  ({obj.position.x:F1}, {obj.position.y:F1})"
                    : $"[{i}]  uid:{obj.uid}  ({obj.position.x:F1}, {obj.position.y:F1})";

                if (GUILayout.Button(label, EditorStyles.label, GUILayout.ExpandWidth(true))) {
                    MapEditorState.SelectedObjectIndex = i;
                    MapEditorState.SelectedTileIndex   = -1;
                    MapEditorState.SelectedHeightIndex = -1;
                    FocusSceneOnTile(obj.position);
                }

                if (GUILayout.Button("✕", GUILayout.Width(24f))) {
                    objects.RemoveAt(i);
                    if (MapEditorState.SelectedObjectIndex >= objects.Count)
                        MapEditorState.SelectedObjectIndex = -1;
                    MapEditorState.IsDirty = true;
                    SceneView.RepaintAll();
                    SirenixEditorGUI.EndBox();
                    EditorGUILayout.EndHorizontal();
                    UnityEngine.GUI.backgroundColor = oldBg;
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (isSelected) {
                    EditorGUI.BeginChangeCheck();
                    var newPos = EditorGUILayout.Vector3Field("Position", obj.position);
                    if (EditorGUI.EndChangeCheck()) {
                        obj.position = newPos;
                        objects[i]   = obj;
                        MapEditorState.IsDirty = true;
                        SceneView.RepaintAll();
                    }
                }

                SirenixEditorGUI.EndBox();
                UnityEngine.GUI.backgroundColor = oldBg;
            }

            GUILayout.Space(4f);
            UnityEngine.GUI.enabled = MapEditorState.SelectedObjectUid != 0;
            if (GUILayout.Button("+ Object 추가 (현재 선택 오브젝트)", GUILayout.Height(26f))) {
                objects.Add(new MapEditorState.WorkingObjectData {
                    uid      = MapEditorState.SelectedObjectUid,
                    position = GetSceneCenter(),
                });
                MapEditorState.SelectedObjectIndex = objects.Count - 1;
                MapEditorState.IsDirty             = true;
                SceneView.RepaintAll();
            }
            UnityEngine.GUI.enabled = true;

            SirenixEditorGUI.EndBox();
        }

        private void DrawHeightPointList() {
            SirenixEditorGUI.BeginBox($"HeightPoints  ({MapEditorState.WorkingHeightPoints.Count})  —  X 기준 오름차순");

            var points = MapEditorState.WorkingHeightPoints;
            for (var i = 0; i < points.Count; i++) {
                var hp        = points[i];
                var isSelected = i == MapEditorState.SelectedHeightIndex;

                var oldBg = UnityEngine.GUI.backgroundColor;
                if (isSelected) UnityEngine.GUI.backgroundColor = new Color(1f, 0.75f, 0.2f, 0.35f);
                SirenixEditorGUI.BeginBox();

                EditorGUILayout.BeginHorizontal();
                var label = $"P{i}  x={hp.x:F1}  groundY={hp.groundY:F1}  [{hp.interpolation}]";
                if (GUILayout.Button(label, EditorStyles.label, GUILayout.ExpandWidth(true))) {
                    MapEditorState.SelectedHeightIndex = i;
                    MapEditorState.SelectedTileIndex   = -1;
                    MapEditorState.SelectedObjectIndex = -1;
                    FocusSceneOnTile(new Vector3(hp.x, hp.groundY, 0f));
                }
                if (GUILayout.Button("✕", GUILayout.Width(24f))) {
                    points.RemoveAt(i);
                    if (MapEditorState.SelectedHeightIndex >= points.Count)
                        MapEditorState.SelectedHeightIndex = -1;
                    MapEditorState.IsDirty = true;
                    SceneView.RepaintAll();
                    SirenixEditorGUI.EndBox();
                    EditorGUILayout.EndHorizontal();
                    UnityEngine.GUI.backgroundColor = oldBg;
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (isSelected) {
                    EditorGUI.BeginChangeCheck();
                    var newX     = EditorGUILayout.FloatField("X", hp.x);
                    var newY     = EditorGUILayout.FloatField("Ground Y", hp.groundY);
                    var newInterp = (HeightInterpolation)EditorGUILayout.EnumPopup("Interpolation", hp.interpolation);
                    if (EditorGUI.EndChangeCheck()) {
                        hp.x             = newX;
                        hp.groundY       = newY;
                        hp.interpolation = newInterp;
                        points[i]        = hp;
                        MapEditorState.WorkingHeightPoints.Sort((a, b) => a.x.CompareTo(b.x));
                        MapEditorState.SelectedHeightIndex =
                            MapEditorState.WorkingHeightPoints.FindIndex(h => h.x == hp.x && h.groundY == hp.groundY);
                        MapEditorState.IsDirty = true;
                        SceneView.RepaintAll();
                    }
                }

                SirenixEditorGUI.EndBox();
                UnityEngine.GUI.backgroundColor = oldBg;
            }

            GUILayout.Space(4f);
            if (GUILayout.Button("+ HeightPoint 추가", GUILayout.Height(26f))) {
                var lastX = points.Count > 0 ? points[^1].x + 10f : 0f;
                points.Add(new HeightPoint { x = lastX, groundY = 0f });
                MapEditorState.SelectedHeightIndex = points.Count - 1;
                MapEditorState.IsDirty             = true;
                SceneView.RepaintAll();
            }

            SirenixEditorGUI.EndBox();
        }

        // ── 내부 유틸 ────────────────────────────────────────────────────

        private void TryLoadTables() {
            var dungeonGuids = AssetDatabase.FindAssets("t:DungeonTable");
            if (dungeonGuids.Length > 0)
                MapEditorState.DungeonTable = AssetDatabase.LoadAssetAtPath<DungeonTable>(
                    AssetDatabase.GUIDToAssetPath(dungeonGuids[0]));

            var phaseGuids = AssetDatabase.FindAssets("t:PhaseTable");
            if (phaseGuids.Length > 0)
                MapEditorState.PhaseTable = AssetDatabase.LoadAssetAtPath<PhaseTable>(
                    AssetDatabase.GUIDToAssetPath(phaseGuids[0]));

            var charGuids = AssetDatabase.FindAssets("t:CharacterTable");
            if (charGuids.Length > 0)
                MapEditorState.CharacterTable = AssetDatabase.LoadAssetAtPath<CharacterTable>(
                    AssetDatabase.GUIDToAssetPath(charGuids[0]));
        }

        private static void RefreshActions() {
            MapEditorState.CurrentMapAction        = null;
            MapEditorState.CurrentEnemySpawnAction = null;
            if (MapEditorState.CurrentPhase == null) return;

            foreach (var action in MapEditorState.CurrentPhase.actions) {
                if (action is MapSpawnAction mapAction)     MapEditorState.CurrentMapAction        = mapAction;
                if (action is EnemySpawnAction enemyAction) MapEditorState.CurrentEnemySpawnAction = enemyAction;
            }
        }

        private static void AddMapSpawnAction() {
            if (MapEditorState.CurrentPhase == null) return;

            Undo.RecordObject(MapEditorState.PhaseTable, "Add MapSpawnAction");

            var actions   = MapEditorState.CurrentPhase.actions?.ToList() ?? new List<ActionBase>();
            var newAction = new MapSpawnAction();
            actions.Add(newAction);
            MapEditorState.CurrentPhase.actions = actions.ToArray();

            MapEditorState.CurrentMapAction = newAction;
            EditorUtility.SetDirty(MapEditorState.PhaseTable);
            AssetDatabase.SaveAssets();
        }

        private static void AddEnemySpawnAction() {
            if (MapEditorState.CurrentPhase == null) return;

            Undo.RecordObject(MapEditorState.PhaseTable, "Add EnemySpawnAction");

            var actions   = MapEditorState.CurrentPhase.actions?.ToList() ?? new List<ActionBase>();
            var newAction = new EnemySpawnAction { spawnData = System.Array.Empty<SpawnData>() };
            actions.Add(newAction);
            MapEditorState.CurrentPhase.actions = actions.ToArray();

            MapEditorState.CurrentEnemySpawnAction = newAction;
            EditorUtility.SetDirty(MapEditorState.PhaseTable);
            AssetDatabase.SaveAssets();
        }

        private static void SaveToPhaseInfo() {
            if (MapEditorState.PhaseTable == null) return;
            if (MapEditorState.CurrentMapAction == null && MapEditorState.CurrentEnemySpawnAction == null) return;

            Undo.RecordObject(MapEditorState.PhaseTable, "Save Map Data");
            MapEditorState.ApplyToMapAction();
            EditorUtility.SetDirty(MapEditorState.PhaseTable);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            MapEditorState.IsDirty = false;
            Debug.Log("[MapEditor] PhaseInfo에 저장 완료.");
        }

        private void RefreshPalette() {
            MapEditorState.PaletteEntries.Clear();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            const string tilePath = "Assets/GAME_ASSET/Prefab/Tile";

            foreach (var group in settings.groups) {
                if (group == null) continue;
                foreach (var entry in group.entries) {
                    var assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    if (string.IsNullOrEmpty(assetPath)) continue;
                    if (!assetPath.EndsWith(".prefab")) continue;
                    if (!assetPath.StartsWith(tilePath)) continue;

                    var preview = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));

                    MapEditorState.PaletteEntries.Add(new MapEditorState.TilePaletteEntry {
                        AddressableGuid = entry.guid,
                        Address         = entry.address,
                        Preview         = preview,
                    });
                }
            }
        }

        private void RefreshObjectPalette() {
            MapEditorState.ObjectPaletteEntries.Clear();
            if (MapEditorState.CharacterTable == null) return;

            var objectTypes = new System.Collections.Generic.HashSet<CharacterType> {
                CharacterType.Obstacle,
                CharacterType.Buff,
                CharacterType.Score,
                CharacterType.Heart,
                CharacterType.Goal,
            };

            foreach (var info in MapEditorState.CharacterTable.CharacterInfos) {
                if (!objectTypes.Contains(info.type)) continue;

                Texture2D preview = null;
                if (!string.IsNullOrEmpty(info.prefab)) {
                    var prefabPath = AssetDatabase.GUIDToAssetPath(info.prefab);
                    if (!string.IsNullOrEmpty(prefabPath)) {
                        var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (go != null) preview = AssetPreview.GetAssetPreview(go);
                    }
                }

                MapEditorState.ObjectPaletteEntries.Add(new MapEditorState.ObjectPaletteEntry {
                    Uid           = info.UID,
                    Label         = $"[{info.UID}] {info.type}",
                    Preview       = preview,
                    CharacterType = info.type,
                });
            }
        }

        private static Color GetTypeColor(CharacterType type) {
            return type switch {
                CharacterType.Obstacle => new Color(1f, 0.3f, 0.3f),
                CharacterType.Buff     => new Color(0.3f, 0.8f, 1f),
                CharacterType.Score    => new Color(1f, 0.85f, 0.2f),
                CharacterType.Heart    => new Color(1f, 0.4f, 0.7f),
                CharacterType.Goal     => new Color(0.3f, 1f, 0.5f),
                _                      => Color.grey,
            };
        }

        private static void FocusSceneOnTile(Vector3 worldPos) {
            if (SceneView.lastActiveSceneView == null) return;
            SceneView.lastActiveSceneView.LookAt(worldPos, Quaternion.identity, 15f);
        }

        private static Vector3 GetSceneCenter() {
            if (SceneView.lastActiveSceneView == null) return Vector3.zero;
            return SceneView.lastActiveSceneView.camera.transform.position + Vector3.forward * 10f;
        }
    }
}
#endif
