#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Script.GameInfo.Character;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using UnityEngine;
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
                    WorkingTiles.Add(new MapTileData { position = t.position, prefabKey = t.prefabKey });

                foreach (var h in CurrentMapAction.heightPoints)
                    WorkingHeightPoints.Add(new HeightPoint { x = h.x, groundY = h.groundY, interpolation = h.interpolation });
            }

            if (CurrentEnemySpawnAction?.spawnData != null) {
                foreach (var s in CurrentEnemySpawnAction.spawnData)
                    WorkingObjects.Add(new WorkingObjectData { uid = s.uid, position = s.position });
            }

            IsDirty = false;
        }

        public static void ApplyToMapAction() {
            if (CurrentMapAction != null) {
                CurrentMapAction.tiles        = WorkingTiles.ToArray();
                CurrentMapAction.heightPoints = WorkingHeightPoints.ToArray();
            }
            if (CurrentEnemySpawnAction != null) {
                CurrentEnemySpawnAction.spawnData = WorkingObjects
                    .Select(o => new SpawnData { uid = o.uid, position = o.position })
                    .ToArray();
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
