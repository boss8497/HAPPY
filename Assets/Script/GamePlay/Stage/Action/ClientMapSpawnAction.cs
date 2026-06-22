using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Dungeon;
using Script.GamePlay.Camera;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public class ClientMapSpawnAction : ClientActionBase {
        private readonly MapSpawnAction _mapSpawnAction;

        // 너비가 측정된 타일 목록 (X 정렬)
        private List<ProcessedTile> _processedTiles;

        // 현재 화면에 스폰된 타일 목록
        private List<ActiveTile> _activeTiles;

        // 다음 스폰 대상 인덱스
        private int _nextSpawnIndex;

        private IStageManager   _stageManager;
        private ICameraControls _cameraControls;

        // ─────────────────────────────────────────────────────────
        // 내부 구조체
        // ─────────────────────────────────────────────────────────

        private readonly struct ProcessedTile {
            public readonly MapTileData Data;

            // MapEditor Save 시 계산된 값을 기획 데이터에서 직접 사용
            public float Width   => Data.width;
            public float CenterX => Data.centerX;
            public float StartX  => Data.startX;
            public float EndX    => Data.endX;

            public ProcessedTile(MapTileData data) {
                Data = data;
            }
        }

        private struct ActiveTile {
            public ProcessedTile Tile;
            public GameObject    Object;
        }

        // ─────────────────────────────────────────────────────────
        // ClientActionBase 구현
        // ─────────────────────────────────────────────────────────

        public ClientMapSpawnAction(ActionBase action) : base(action) {
            if (action is MapSpawnAction mapSpawnAction)
                _mapSpawnAction = mapSpawnAction;
        }

        public override void Initialize(IStageManager stageManager) {
            _stageManager   = stageManager;
            _cameraControls = stageManager.CameraControls;
            _nextSpawnIndex = 0;
            _activeTiles    = new List<ActiveTile>();
            _processedTiles = BuildProcessedTiles();
        }

        public override async UniTask ExecuteAsync() {
            DespawnOffScreenTiles();
            SpawnVisibleTiles();
            UpdateCurrentGroundY();

            await UniTask.CompletedTask;
        }

        public override void Release() {
            if (_activeTiles != null) {
                foreach (var active in _activeTiles) {
                    if (active.Object != null)
                        _stageManager.StagePooling.Push(active.Object);
                }
                _activeTiles.Clear();
            }

            _processedTiles?.Clear();
            _stageManager   = null;
            _cameraControls = null;
        }

        // ─────────────────────────────────────────────────────────
        // 타일 스폰 / 디스폰
        // ─────────────────────────────────────────────────────────

        private void SpawnVisibleTiles() {
            if (_processedTiles == null) return;

            while (_nextSpawnIndex < _processedTiles.Count) {
                var tile = _processedTiles[_nextSpawnIndex];

                // 타일 왼쪽 끝이 SpawnOffset을 넘으면 아직 스폰 불필요
                if (tile.StartX > _cameraControls.SpawnOffset) break;

                GameObject obj = null;
                if (!string.IsNullOrEmpty(tile.Data.prefabKey)) {
                    obj = _stageManager.StagePooling.Pop(tile.Data.prefabKey);
                    if (obj != null)
                        obj.transform.position = tile.Data.position; // 중앙 위치 그대로 사용
                }

                _activeTiles.Add(new ActiveTile { Tile = tile, Object = obj });
                _nextSpawnIndex++;
            }
        }

        private void DespawnOffScreenTiles() {
            var leftBound = _cameraControls.OutSideLeftX;

            for (var i = _activeTiles.Count - 1; i >= 0; i--) {
                var active = _activeTiles[i];
                if (active.Tile.EndX >= leftBound) continue;

                if (active.Object != null)
                    _stageManager.StagePooling.Push(active.Object);

                _activeTiles.RemoveAt(i);
            }
        }

        // ─────────────────────────────────────────────────────────
        // HeightPoint 커브 기반 바닥 Y 계산 → StageManager에 전달
        // ─────────────────────────────────────────────────────────

        private void UpdateCurrentGroundY() {
            var players = _stageManager.Players;
            if (players == null || players.Count == 0) return;

            var playerX = players[0].Transform.position.x;
            _stageManager.SetGroundY(ComputeGroundY(playerX));
        }

        // HeightPoint 배열에서 playerX에 해당하는 바닥 Y를 보간하여 반환
        //
        //  P0(x=0, y=0) → P1(x=50, y=0)  → P2(x=100, y=10)
        //   playerX=25  : 0 (P0-P1 구간, y 동일)
        //   playerX=75  : Lerp(0, 10, 0.5) = 5
        private float ComputeGroundY(float playerX) {
            var points = _mapSpawnAction?.heightPoints;
            if (points == null || points.Length == 0) return 0f;

            // 첫 포인트 이전 구간: 첫 포인트 Y 유지
            if (playerX <= points[0].x) return points[0].groundY;

            // 마지막 포인트 이후 구간: 마지막 포인트 Y 유지
            if (playerX >= points[points.Length - 1].x) return points[points.Length - 1].groundY;

            // Point i → Point i+1 구간 탐색 후 보간
            for (var i = 0; i < points.Length - 1; i++) {
                var p0 = points[i];
                var p1 = points[i + 1];

                if (playerX < p0.x || playerX > p1.x) continue;

                var t = (playerX - p0.x) / (p1.x - p0.x);
                return Interpolate(p0.groundY, p1.groundY, t, p0.interpolation);
            }

            return points[points.Length - 1].groundY;
        }

        private static float Interpolate(float from, float to, float t, HeightInterpolation interpolation) {
            return interpolation switch {
                HeightInterpolation.Linear => Mathf.Lerp(from, to, t),
                _                         => Mathf.Lerp(from, to, t),
            };
        }

        // ─────────────────────────────────────────────────────────
        // 타일 목록 전처리: X 정렬 (너비/범위는 MapEditor Save 시 기획 데이터에 저장됨)
        // ─────────────────────────────────────────────────────────

        private List<ProcessedTile> BuildProcessedTiles() {
            if (_mapSpawnAction?.tiles == null || _mapSpawnAction.tiles.Length == 0)
                return new List<ProcessedTile>();

            var sorted = new List<MapTileData>(_mapSpawnAction.tiles);
            sorted.Sort((a, b) => a.position.x.CompareTo(b.position.x));

            var result = new List<ProcessedTile>(sorted.Count);
            foreach (var tileData in sorted)
                result.Add(new ProcessedTile(tileData));

            return result;
        }
    }
}
