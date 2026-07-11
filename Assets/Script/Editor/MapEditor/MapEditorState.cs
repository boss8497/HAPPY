#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Script.GameInfo.Character;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.Editor.MapEditor {
    public enum MapEditMode {
        Select,
        TilePlacement,
        ObjectPlacement,
        HeightPoint,
    }

    public static class MapEditorState {
        // ── 데이터 선택 ──────────────────────────────────────────────────
        public static DungeonTable   DungeonTable;
        public static PhaseTable     PhaseTable;
        public static CharacterTable CharacterTable;

        public static int  SelectedDungeonIndex = -1;
        public static int  SelectedStageIndex   = -1;
        public static int  SelectedPhaseUid     = -1;

        public static DungeonInfo    CurrentDungeon;
        public static Stage          CurrentStage;
        public static PhaseInfo      CurrentPhase;
        public static MapSpawnAction   CurrentMapAction;
        public static EnemySpawnAction CurrentEnemySpawnAction;

        // ── 작업 중인 데이터 (Save 시 각 Action에 기록) ──────────────────
        public static List<MapTileData>       WorkingTiles        = new();
        public static List<HeightPoint>       WorkingHeightPoints = new();
        public static List<WorkingObjectData> WorkingObjects      = new();

        // ── 편집 상태 ────────────────────────────────────────────────────
        public static MapEditMode EditMode          = MapEditMode.Select;
        public static string      SelectedPrefabKey = string.Empty;
        public static int         SelectedObjectUid = 0;
        public static int         SelectedTileIndex   = -1;
        public static int         SelectedHeightIndex = -1;
        public static int         SelectedObjectIndex = -1;

        // ── 팔레트 캐시 ──────────────────────────────────────────────────
        public static List<TilePaletteEntry>   PaletteEntries       = new();
        public static List<ObjectPaletteEntry> ObjectPaletteEntries = new();

        public static bool IsDirty;

        public static void ClearSelection() {
            SelectedTileIndex   = -1;
            SelectedHeightIndex = -1;
            SelectedObjectIndex = -1;
        }

        public static void LoadFromMapAction() {
            WorkingTiles.Clear();
            WorkingHeightPoints.Clear();
            WorkingObjects.Clear();

            if (CurrentMapAction != null) {
                foreach (var t in CurrentMapAction.tiles)
                    WorkingTiles.Add(new MapTileData {
                        position = t.position,
                        prefabKey = t.prefabKey,
                        width    = t.width,
                        centerX  = t.centerX,
                        startX   = t.startX,
                        endX     = t.endX,
                    });

                foreach (var h in CurrentMapAction.heightPoints)
                    WorkingHeightPoints.Add(new HeightPoint {
                        x             = h.x,
                        groundY       = h.groundY,
                        interpolation = h.interpolation,
                        hasFallDeathY = h.hasFallDeathY,
                        fallDeathY    = h.fallDeathY,
                    });
            }

            if (CurrentEnemySpawnAction?.spawnData != null) {
                foreach (var s in CurrentEnemySpawnAction.spawnData)
                    WorkingObjects.Add(new WorkingObjectData { uid = s.uid, position = s.position });
            }

            IsDirty = false;
        }

        public static void ApplyToMapAction() {
            if (CurrentMapAction != null) {
                foreach (var tile in WorkingTiles)
                    ComputeTileBounds(tile);

                CurrentMapAction.tiles        = WorkingTiles.ToArray();
                CurrentMapAction.heightPoints = WorkingHeightPoints.ToArray();
            }
            if (CurrentEnemySpawnAction != null) {
                CurrentEnemySpawnAction.spawnData = WorkingObjects
                    .OrderBy(o => o.position.x)
                    .Select(o => new SpawnData { uid = o.uid, position = new Vector3(o.position.x, o.position.y, 0f) })
                    .ToArray();
            }
        }

        // Save 시 prefab을 임시 인스턴스화해 Tilemap.cellBounds 기준으로 너비/범위 계산
        private static void ComputeTileBounds(MapTileData tile) {
            if (string.IsNullOrEmpty(tile.prefabKey)) {
                tile.width   = 0f;
                tile.centerX = tile.position.x;
                tile.startX  = tile.position.x;
                tile.endX    = tile.position.x;
                return;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(tile.prefabKey);
            var prefab    = string.IsNullOrEmpty(assetPath)
                          ? null
                          : AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) {
                tile.width   = 0f;
                tile.centerX = tile.position.x;
                tile.startX  = tile.position.x;
                tile.endX    = tile.position.x;
                return;
            }

            // 임시 인스턴스화 (HideAndDontSave → 씬에 저장 안 됨)
            var go = Object.Instantiate(prefab, tile.position, Quaternion.identity);
            go.hideFlags = HideFlags.HideAndDontSave;

            try {
                float width   = 0f;
                float centerX = tile.position.x;
                
                // TitleMap 우선
                var tilemap = go.GetComponentInChildren<Tilemap>(true);
                if (tilemap != null && tilemap.cellBounds.size.x > 0) {
                    if (tilemap != null && tilemap.cellBounds.size.x > 0) {
                        var scale       = Mathf.Abs(tilemap.transform.lossyScale.x);
                        width   = tilemap.cellBounds.size.x * scale;
                        centerX = tilemap.transform.position.x + tilemap.cellBounds.center.x;
                    }
                } else {
                    var sr = go.GetComponentInChildren<SpriteRenderer>(true);
                    width   = sr.bounds.size.x;
                    centerX = sr.bounds.center.x;
                }

                tile.width   = width;
                tile.centerX = centerX;
                tile.startX  = centerX - width * 0.5f;
                tile.endX    = centerX + width * 0.5f;
            }
            finally {
                Object.DestroyImmediate(go);
            }
        }

        // ── 구조체 ──────────────────────────────────────────────────────
        public struct TilePaletteEntry {
            public string    AddressableGuid;
            public string    Address;
            public Texture2D Preview;
        }

        public struct ObjectPaletteEntry {
            public int           Uid;
            public string        Label;
            public Texture2D     Preview;
            public CharacterType CharacterType;
        }

        public struct WorkingObjectData {
            public int     uid;
            public Vector3 position;
        }
    }
}
#endif
