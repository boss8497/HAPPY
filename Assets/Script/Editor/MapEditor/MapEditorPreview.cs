#if UNITY_EDITOR
using System.Collections.Generic;
using Script.GameInfo.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.MapEditor {
    /// <summary>
    /// MapEditor 작업 데이터(WorkingTiles, WorkingObjects)에 대응하는
    /// 씬 프리뷰 GameObject를 관리한다.
    /// - HideAndDontSave 플래그로 씬에 저장되지 않음
    /// - Assembly-CSharp 소속 MonoBehaviour는 에디터 오류 방지를 위해 비활성화
    /// - Spine/Unity 내장 컴포넌트는 유지해 실제 모양이 보이도록 함
    /// </summary>
    public static class MapEditorPreview {
        private static GameObject              _root;
        private static readonly List<GameObject> _tileGOs   = new();
        private static readonly List<GameObject> _objectGOs = new();

        // 마지막으로 동기화한 상태 캐시
        private static int           _lastTileCount   = -1;
        private static int           _lastObjectCount = -1;
        private static MapSpawnAction   _lastMapAction;
        private static EnemySpawnAction _lastEnemyAction;

        // ── 공개 API ─────────────────────────────────────────────────────

        /// <summary>매 OnSceneGUI 에서 호출. 구조 변경 시 재빌드, 아니면 위치만 동기화.</summary>
        public static void Sync() {
            var tileCount   = MapEditorState.WorkingTiles.Count;
            var objectCount = MapEditorState.WorkingObjects.Count;

            var needRebuild = _lastTileCount    != tileCount
                           || _lastObjectCount  != objectCount
                           || _lastMapAction    != MapEditorState.CurrentMapAction
                           || _lastEnemyAction  != MapEditorState.CurrentEnemySpawnAction
                           || HasNullGO();

            if (needRebuild) {
                _lastTileCount   = tileCount;
                _lastObjectCount = objectCount;
                _lastMapAction   = MapEditorState.CurrentMapAction;
                _lastEnemyAction = MapEditorState.CurrentEnemySpawnAction;
                Rebuild();
            } else {
                SyncPositions();
            }
        }

        /// <summary>에디터 창 닫힐 때 또는 페이즈 변경 시 호출.</summary>
        public static void Cleanup() {
            DestroyAll();
            _lastTileCount   = -1;
            _lastObjectCount = -1;
            _lastMapAction   = null;
            _lastEnemyAction = null;
        }

        // ── 내부 구현 ────────────────────────────────────────────────────

        private static bool HasNullGO() {
            foreach (var go in _tileGOs)   if (!go) return true;
            foreach (var go in _objectGOs) if (!go) return true;
            return false;
        }

        private static void Rebuild() {
            DestroyAll();

            _root = new GameObject("[MapEditor Preview]") {
                hideFlags = HideFlags.HideAndDontSave
            };

            foreach (var tile in MapEditorState.WorkingTiles)
                _tileGOs.Add(CreateTileGO(tile));

            foreach (var obj in MapEditorState.WorkingObjects)
                _objectGOs.Add(CreateObjectGO(obj));
        }

        private static void SyncPositions() {
            var tiles   = MapEditorState.WorkingTiles;
            var objects = MapEditorState.WorkingObjects;

            for (var i = 0; i < _tileGOs.Count && i < tiles.Count; i++) {
                if (_tileGOs[i]) _tileGOs[i].transform.position = tiles[i].position;
            }
            for (var i = 0; i < _objectGOs.Count && i < objects.Count; i++) {
                if (_objectGOs[i]) _objectGOs[i].transform.position = objects[i].position;
            }
        }

        private static void DestroyAll() {
            foreach (var go in _tileGOs)   if (go) Object.DestroyImmediate(go);
            foreach (var go in _objectGOs) if (go) Object.DestroyImmediate(go);
            _tileGOs.Clear();
            _objectGOs.Clear();
            if (_root) Object.DestroyImmediate(_root);
            _root = null;
        }

        private static GameObject CreateTileGO(MapTileData tile) {
            if (string.IsNullOrEmpty(tile.prefabKey)) return null;
            var path   = AssetDatabase.GUIDToAssetPath(tile.prefabKey);
            var prefab = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab ? InstantiatePreview(prefab, tile.position) : null;
        }

        private static GameObject CreateObjectGO(MapEditorState.WorkingObjectData obj) {
            if (MapEditorState.CharacterTable == null) return null;
            var info = System.Array.Find(MapEditorState.CharacterTable.CharacterInfos, c => c.UID == obj.uid);
            if (info == null || string.IsNullOrEmpty(info.prefab)) return null;
            var path   = AssetDatabase.GUIDToAssetPath(info.prefab);
            var prefab = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab ? InstantiatePreview(prefab, obj.position) : null;
        }

        private static GameObject InstantiatePreview(GameObject prefab, Vector3 position) {
            try {
                var go = Object.Instantiate(prefab, position, Quaternion.identity);
                if (_root) go.transform.SetParent(_root.transform);

                // 씬에 저장되지 않도록, Hierarchy에서 숨김
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                    t.gameObject.hideFlags = HideFlags.HideAndDontSave;

                // 게임 전용 스크립트만 비활성화 (Spine, Unity 내장 제외)
                foreach (var mono in go.GetComponentsInChildren<MonoBehaviour>(true)) {
                    if (mono == null) continue;
                    var asmName = mono.GetType().Assembly.GetName().Name;
                    if (asmName == "Assembly-CSharp") mono.enabled = false;
                }

                return go;
            }
            catch {
                return null;
            }
        }
    }
}
#endif
