#if UNITY_EDITOR
using System.Linq;
using Script.GameInfo.Character;
using Script.GameInfo.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.MapEditor {
    [InitializeOnLoad]
    public static class MapEditorSceneGUI {
        private static bool _registered;

        static MapEditorSceneGUI() { }

        public static void Register() {
            if (_registered) return;
            SceneView.duringSceneGui += OnSceneGUI;
            _registered = true;
        }

        public static void Unregister() {
            SceneView.duringSceneGui -= OnSceneGUI;
            _registered = false;
            MapEditorPreview.Cleanup();
        }

        private static void OnSceneGUI(SceneView sceneView) {
            if (MapEditorState.CurrentMapAction == null && MapEditorState.CurrentEnemySpawnAction == null) {
                MapEditorPreview.Cleanup();
                return;
            }

            // 프리팹 프리뷰 동기화 (구조 변경 시 재빌드, 아니면 위치만 갱신)
            MapEditorPreview.Sync();

            DrawFallDeathLine();
            DrawHeightPointCurve();
            DrawTileHandles();
            DrawObjectHandles();
            HandleInput(sceneView);
        }

        // ── 낙사 Y 라인 (구간별) ─────────────────────────────────────────

        private static void DrawFallDeathLine() {
            var points = MapEditorState.WorkingHeightPoints;
            if (points == null || points.Count < 2) return;

            var labelStyle = new GUIStyle(EditorStyles.miniLabel) {
                normal = { textColor = new Color(1f, 0.3f, 0.3f) }
            };

            for (var i = 0; i < points.Count - 1; i++) {
                var p0 = points[i];
                if (!p0.hasFallDeathY) continue;

                var x0    = p0.x;
                var x1    = points[i + 1].x;
                var y     = p0.fallDeathY;
                var left  = new Vector3(x0, y, 0f);
                var right = new Vector3(x1, y, 0f);

                Handles.color = new Color(1f, 0.15f, 0.15f, 0.9f);
                Handles.DrawLine(left, right, 2f);
                Handles.Label(right + Vector3.up * 0.3f, $"fallDeathY={y:F1}", labelStyle);
            }
        }

        // ── HeightPoint 커브 (Gizmos 유지) ───────────────────────────────

        private static void DrawHeightPointCurve() {
            var points = MapEditorState.WorkingHeightPoints;
            if (points == null || points.Count == 0) return;

            for (var i = 0; i < points.Count; i++) {
                var hp       = points[i];
                var worldPos = new Vector3(hp.x, hp.groundY, 0f);

                var isSelected = i == MapEditorState.SelectedHeightIndex;
                var size       = HandleUtility.GetHandleSize(worldPos) * 0.12f;
                var color      = isSelected ? Color.yellow : new Color(1f, 0.55f, 0.1f);

                Handles.color = color;

                if (Handles.Button(worldPos, Quaternion.identity, size, size * 1.5f, Handles.SphereHandleCap)) {
                    MapEditorState.SelectedHeightIndex = i;
                    MapEditorState.SelectedTileIndex   = -1;
                    MapEditorState.SelectedObjectIndex = -1;
                    UnityEngine.GUI.changed            = true;
                }

                if (isSelected) {
                    EditorGUI.BeginChangeCheck();
                    var newPos = Handles.PositionHandle(worldPos, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck()) {
                        hp.x       = newPos.x;
                        hp.groundY = newPos.y;
                        points[i]  = hp;
                        MapEditorState.IsDirty = true;

                        MapEditorState.WorkingHeightPoints.Sort((a, b) => a.x.CompareTo(b.x));
                        MapEditorState.SelectedHeightIndex =
                            MapEditorState.WorkingHeightPoints.FindIndex(h => h.x == hp.x && h.groundY == hp.groundY);
                    }
                }

                var labelStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color } };
                Handles.Label(worldPos + Vector3.up * (size * 2f),
                    $"P{i}  x={hp.x:F1}  y={hp.groundY:F1}", labelStyle);
            }

            // 포인트 간 연결선
            Handles.color = new Color(0.2f, 1f, 0.4f, 0.85f);
            for (var i = 0; i < points.Count - 1; i++) {
                var p0 = new Vector3(points[i].x,     points[i].groundY,     0f);
                var p1 = new Vector3(points[i + 1].x, points[i + 1].groundY, 0f);
                DrawGroundSegment(p0, p1, 24);
            }

            if (points.Count >= 2) {
                for (var i = 0; i < points.Count - 1; i++) {
                    var mid = new Vector3(
                        (points[i].x       + points[i + 1].x)       * 0.5f,
                        (points[i].groundY + points[i + 1].groundY) * 0.5f,
                        0f);
                    var dir       = new Vector3(points[i + 1].x - points[i].x, points[i + 1].groundY - points[i].groundY, 0f).normalized;
                    var arrowSize = HandleUtility.GetHandleSize(mid) * 0.08f;
                    Handles.color = new Color(0.2f, 1f, 0.4f, 0.6f);
                    Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir, Vector3.forward), arrowSize, EventType.Repaint);
                }
            }
        }

        private static void DrawGroundSegment(Vector3 p0, Vector3 p1, int steps) {
            var prev = p0;
            for (var s = 1; s <= steps; s++) {
                var next = Vector3.Lerp(p0, p1, s / (float)steps);
                Handles.DrawLine(prev, next, 2f);
                prev = next;
            }
        }

        // ── 타일 핸들 (프리팹이 시각 표현, 핸들은 선택/이동용) ──────────

        private static void DrawTileHandles() {
            var tiles = MapEditorState.WorkingTiles;
            if (tiles == null) return;

            for (var i = 0; i < tiles.Count; i++) {
                var tile       = tiles[i];
                var worldPos   = tile.position;
                var isSelected = i == MapEditorState.SelectedTileIndex;
                var size       = HandleUtility.GetHandleSize(worldPos) * 0.10f;

                // 선택된 타일: 밝은 파랑 하이라이트 링
                if (isSelected) {
                    Handles.color = new Color(0.3f, 0.7f, 1f, 0.85f);
                    Handles.DrawWireDisc(worldPos, Vector3.forward, HandleUtility.GetHandleSize(worldPos) * 0.55f);
                }

                Handles.color = isSelected
                    ? new Color(0.3f, 0.8f, 1f, 1f)
                    : new Color(0.6f, 0.85f, 1f, 0.6f);

                if (Handles.Button(worldPos, Quaternion.identity, size, size * 1.6f, Handles.DotHandleCap)) {
                    MapEditorState.SelectedTileIndex   = i;
                    MapEditorState.SelectedHeightIndex = -1;
                    MapEditorState.SelectedObjectIndex = -1;
                    UnityEngine.GUI.changed            = true;
                }

                if (isSelected) {
                    EditorGUI.BeginChangeCheck();
                    var newPos = Handles.PositionHandle(worldPos, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck()) {
                        tile.position = newPos;
                        tiles[i]      = tile;
                        MapEditorState.IsDirty = true;
                    }

                    // 선택된 경우에만 이름 표시
                    if (!string.IsNullOrEmpty(tile.prefabKey)) {
                        var assetPath = AssetDatabase.GUIDToAssetPath(tile.prefabKey);
                        if (!string.IsNullOrEmpty(assetPath)) {
                            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                            Handles.Label(worldPos + Vector3.up * (HandleUtility.GetHandleSize(worldPos) * 0.65f), name,
                                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.cyan } });
                        }
                    }
                }
            }
        }

        // ── 오브젝트 핸들 (프리팹이 시각 표현, 핸들은 선택/이동용) ──────

        private static void DrawObjectHandles() {
            var objects = MapEditorState.WorkingObjects;
            if (objects == null) return;

            for (var i = 0; i < objects.Count; i++) {
                var obj        = objects[i];
                var worldPos   = obj.position;
                var isSelected = i == MapEditorState.SelectedObjectIndex;
                var size       = HandleUtility.GetHandleSize(worldPos) * 0.10f;
                var typeColor  = GetObjectColor(obj.uid);

                // 선택된 오브젝트: 타입 색상 하이라이트 링
                if (isSelected) {
                    Handles.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.85f);
                    Handles.DrawWireDisc(worldPos, Vector3.forward, HandleUtility.GetHandleSize(worldPos) * 0.55f);
                }

                Handles.color = isSelected ? typeColor : new Color(typeColor.r, typeColor.g, typeColor.b, 0.6f);

                if (Handles.Button(worldPos, Quaternion.identity, size, size * 1.6f, Handles.DotHandleCap)) {
                    MapEditorState.SelectedObjectIndex = i;
                    MapEditorState.SelectedTileIndex   = -1;
                    MapEditorState.SelectedHeightIndex = -1;
                    UnityEngine.GUI.changed            = true;
                }

                if (isSelected) {
                    EditorGUI.BeginChangeCheck();
                    var newPos = Handles.PositionHandle(worldPos, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck()) {
                        obj.position = newPos;
                        objects[i]   = obj;
                        MapEditorState.IsDirty = true;
                    }

                    Handles.Label(worldPos + Vector3.up * (HandleUtility.GetHandleSize(worldPos) * 0.65f),
                        GetObjectLabel(obj.uid),
                        new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = typeColor } });
                }
            }
        }

        private static Color GetObjectColor(int uid) {
            if (MapEditorState.CharacterTable == null) return new Color(0.3f, 1f, 0.5f);
            var info = System.Array.Find(MapEditorState.CharacterTable.CharacterInfos, c => c.UID == uid);
            if (info == null) return new Color(0.3f, 1f, 0.5f);
            return info.type switch {
                CharacterType.Obstacle => new Color(1f, 0.3f, 0.3f),
                CharacterType.Buff     => new Color(0.3f, 0.8f, 1f),
                CharacterType.Score    => new Color(1f, 0.85f, 0.2f),
                CharacterType.Heart    => new Color(1f, 0.4f, 0.7f),
                CharacterType.Goal     => new Color(0.3f, 1f, 0.5f),
                _                      => new Color(0.3f, 1f, 0.5f),
            };
        }

        private static string GetObjectLabel(int uid) {
            if (MapEditorState.CharacterTable == null) return $"uid:{uid}";
            var info = System.Array.Find(MapEditorState.CharacterTable.CharacterInfos, c => c.UID == uid);
            return info != null ? $"{info.type}  uid:{uid}" : $"uid:{uid}";
        }

        // ── 입력 처리 ────────────────────────────────────────────────────

        private static void HandleInput(SceneView sceneView) {
            var evt = Event.current;

            if (MapEditorState.EditMode == MapEditMode.HeightPoint) {
                if (evt.type == EventType.MouseDown && evt.button == 0 && evt.shift) {
                    var worldPos = GetSceneWorldPos(evt.mousePosition, sceneView);
                    var newPoint = new HeightPoint { x = worldPos.x, groundY = worldPos.y };
                    MapEditorState.WorkingHeightPoints.Add(newPoint);
                    MapEditorState.WorkingHeightPoints.Sort((a, b) => a.x.CompareTo(b.x));
                    MapEditorState.IsDirty = true;
                    evt.Use();
                    SceneView.RepaintAll();
                }

                if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Delete) {
                    var idx = MapEditorState.SelectedHeightIndex;
                    if (idx >= 0 && idx < MapEditorState.WorkingHeightPoints.Count) {
                        MapEditorState.WorkingHeightPoints.RemoveAt(idx);
                        MapEditorState.SelectedHeightIndex = -1;
                        MapEditorState.IsDirty             = true;
                        evt.Use();
                        SceneView.RepaintAll();
                    }
                }
            }

            if (MapEditorState.EditMode == MapEditMode.TilePlacement) {
                if (!string.IsNullOrEmpty(MapEditorState.SelectedPrefabKey) &&
                    evt.type == EventType.MouseDown && evt.button == 0 && !evt.alt) {
                    var worldPos = GetSceneWorldPos(evt.mousePosition, sceneView);
                    worldPos.z = 0f;
                    MapEditorState.WorkingTiles.Add(new MapTileData {
                        position  = worldPos,
                        prefabKey = MapEditorState.SelectedPrefabKey
                    });
                    MapEditorState.IsDirty = true;
                    evt.Use();
                    SceneView.RepaintAll();
                }

                if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Delete) {
                    var idx = MapEditorState.SelectedTileIndex;
                    if (idx >= 0 && idx < MapEditorState.WorkingTiles.Count) {
                        MapEditorState.WorkingTiles.RemoveAt(idx);
                        MapEditorState.SelectedTileIndex = -1;
                        MapEditorState.IsDirty           = true;
                        evt.Use();
                        SceneView.RepaintAll();
                    }
                }
            }

            if (MapEditorState.EditMode == MapEditMode.ObjectPlacement) {
                if (MapEditorState.SelectedObjectUid != 0 &&
                    evt.type == EventType.MouseDown && evt.button == 0 && !evt.alt) {
                    var worldPos = GetSceneWorldPos(evt.mousePosition, sceneView);
                    worldPos.z = 0f;
                    MapEditorState.WorkingObjects.Add(new MapEditorState.WorkingObjectData {
                        uid      = MapEditorState.SelectedObjectUid,
                        position = worldPos,
                    });
                    MapEditorState.IsDirty = true;
                    evt.Use();
                    SceneView.RepaintAll();
                }

                if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Delete) {
                    var idx = MapEditorState.SelectedObjectIndex;
                    if (idx >= 0 && idx < MapEditorState.WorkingObjects.Count) {
                        MapEditorState.WorkingObjects.RemoveAt(idx);
                        MapEditorState.SelectedObjectIndex = -1;
                        MapEditorState.IsDirty             = true;
                        evt.Use();
                        SceneView.RepaintAll();
                    }
                }
            }

            if (MapEditorState.EditMode == MapEditMode.Select) {
                if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Delete) {
                    if (MapEditorState.SelectedTileIndex >= 0) {
                        MapEditorState.WorkingTiles.RemoveAt(MapEditorState.SelectedTileIndex);
                        MapEditorState.SelectedTileIndex = -1;
                        MapEditorState.IsDirty           = true;
                        evt.Use();
                        SceneView.RepaintAll();
                    }
                    else if (MapEditorState.SelectedObjectIndex >= 0) {
                        MapEditorState.WorkingObjects.RemoveAt(MapEditorState.SelectedObjectIndex);
                        MapEditorState.SelectedObjectIndex = -1;
                        MapEditorState.IsDirty             = true;
                        evt.Use();
                        SceneView.RepaintAll();
                    }
                    else if (MapEditorState.SelectedHeightIndex >= 0) {
                        MapEditorState.WorkingHeightPoints.RemoveAt(MapEditorState.SelectedHeightIndex);
                        MapEditorState.SelectedHeightIndex = -1;
                        MapEditorState.IsDirty             = true;
                        evt.Use();
                        SceneView.RepaintAll();
                    }
                }
            }
        }

        private static Vector3 GetSceneWorldPos(Vector2 mousePosition, SceneView sceneView) {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var t   = ray.origin.z == 0f ? 0f : -ray.origin.z / ray.direction.z;
            return ray.GetPoint(t);
        }
    }
}
#endif
