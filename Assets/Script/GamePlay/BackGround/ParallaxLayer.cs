using System;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Background {
    [DisallowMultipleComponent]
    public class ParallaxLayer : MonoBehaviour {
        [Serializable]
        public class Tile {
            public Transform      transform;
            public SpriteRenderer spriteRenderer;

            public float Width
                => spriteRenderer != null
                       ? spriteRenderer.bounds.size.x
                       : 0f;

            public bool IsValid => transform != null && spriteRenderer != null;
        }

        [Header("Parallax")]
        [SerializeField, Range(0f, 1.5f)]
        private float _parallaxFactor = 1f;

        [SerializeField]
        private float _startXOffset = 0f;

        [Header("Loop")]
        [SerializeField]
        private bool _loop = true;

        [SerializeField]
        private bool _autoAlignOnStart = true;

        [SerializeField]
        private float _recycleOffset = 0f;

        [SerializeField]
        private float _cameraLeftOffset = 0f;

        [Header("Tiles (left -> right order)")]
        [SerializeField]
        private Tile[] _tiles;

        // Target 기준 상대 오프셋
        private Vector3 _relativeOffset;
        private float   _fixedY;
        private float   _fixedZ;

        // Canonical layout (한 사이클 기준 정답 배치)
        private float[] _baseCenters;
        private float[] _baseRights;
        private int     _tileCount;
        private float   _cycleWidth;

        private bool _initialized;

        public float ParallaxFactor => _parallaxFactor;
        public bool  LoopEnabled    => _loop;
        public bool  Initialized    => _initialized;

        public void Initialize(Vector3 targetPos) {
            InitializeTiles();
            SetOffset();

            if (_autoAlignOnStart) {
                AlignTiles();
            }
            else {
                NormalizeLayout();
            }

            CacheLayout();
            CaptureRelativeOffset(targetPos);

            if (_loop) {
                // 초기 1회 강제 배치
                // Initialize 직후에는 Tick에서 바로 다시 정렬되므로,
                // 여기서는 0을 넣어도 괜찮다.
                UpdateLoop(targetPos, 0);
            }

            _initialized = true;
        }

        public void Rebind(Vector3 targetPos) {
            CaptureRelativeOffset(targetPos);
        }

        public void Tick(Vector3 targetPos, float cameraLeftX) {
            if (_initialized == false)
                return;

            UpdateParallax(targetPos);

            if (_loop) {
                UpdateLoop(targetPos, cameraLeftX);
            }
        }

        private void InitializeTiles() {
            if (_tiles == null || _tiles.Length == 0) {
                _tileCount = 0;
                return;
            }

            foreach (var tile in _tiles) {
                if (tile == null || tile.transform == null)
                    continue;

                if (tile.spriteRenderer == null) {
                    tile.spriteRenderer = tile.transform.GetComponent<SpriteRenderer>();
                }

                if (tile.transform.parent != transform) {
                    tile.transform.SetParent(transform, true);
                }
            }

            SortTiles();
        }

        private void SetOffset() {
            var pos = transform.position;
            pos.x              += _startXOffset;
            transform.position =  pos;
        }

        private void CaptureRelativeOffset(Vector3 targetPos) {
            var pos = transform.position;

            _fixedY = pos.y;
            _fixedZ = pos.z;

            _relativeOffset = new(
                pos.x - (targetPos.x * _parallaxFactor),
                _fixedY,
                _fixedZ
            );
        }

        private float GetCurrentRootX(Vector3 targetPos) {
            return _relativeOffset.x + (targetPos.x * _parallaxFactor);
        }

        private void UpdateParallax(Vector3 targetPos) {
            var pos = transform.position;
            pos.x              = GetCurrentRootX(targetPos);
            pos.y              = _fixedY;
            pos.z              = _fixedZ;
            transform.position = pos;
        }

        private void UpdateLoop(Vector3 targetPos, float cameraLeftX) {
            if (_tileCount <= 0 || _cycleWidth <= 0f)
                return;

            var rootX = GetCurrentRootX(targetPos);

            // 화면 왼쪽보다 조금 더 왼쪽 지점까지 채우도록 기준점 계산
            var recycleX     = cameraLeftX + _cameraLeftOffset - _recycleOffset;
            var recycleLocal = recycleX - rootX;

            // recycleLocal 이 어떤 cycle 에 속하는지
            var cycleIndex   = Mathf.FloorToInt(recycleLocal / _cycleWidth);
            var localInCycle = recycleLocal - (cycleIndex * _cycleWidth);

            if (localInCycle < 0f) {
                localInCycle += _cycleWidth;
                cycleIndex--;
            }

            // 현재 recycle 지점을 포함하는 tile 이 "front"
            var frontIndex = FindFrontTileIndex(localInCycle);

            // frontIndex tile은 현재 cycleIndex에 놓이고,
            // 그 앞쪽(tile 0~frontIndex-1)은 다음 cycleIndex + 1 로 넘어감
            // => "첫 번째 Tile을 마지막에 이어 붙이는 구조"를 매 프레임 정답으로 재구성
            for (var order = 0; order < _tileCount; order++) {
                var tileIndex = (frontIndex + order) % _tileCount;
                var tile      = _tiles[tileIndex];

                if (tile == null || tile.IsValid == false)
                    continue;

                var wrap           = tileIndex < frontIndex ? 1 : 0;
                var tileCycleIndex = cycleIndex + wrap;

                var newLocalX = _baseCenters[tileIndex] + (tileCycleIndex * _cycleWidth);
                tile.transform.SetLocalPositionX(newLocalX);
            }
        }

        private int FindFrontTileIndex(float localInCycle) {
            for (var i = 0; i < _tileCount; i++) {
                if (localInCycle < _baseRights[i]) {
                    return i;
                }
            }

            return 0;
        }

        private void CacheLayout() {
            if (_tiles == null || _tiles.Length == 0) {
                _tileCount   = 0;
                _baseCenters = Array.Empty<float>();
                _baseRights  = Array.Empty<float>();
                _cycleWidth  = 0f;
                return;
            }

            var validCount = 0;
            foreach (var tile in _tiles) {
                if (tile != null && tile.IsValid) {
                    validCount++;
                }
            }

            _tileCount   = validCount;
            _baseCenters = new float[_tileCount];
            _baseRights  = new float[_tileCount];

            var dst = 0;
            foreach (var tile in _tiles) {
                if (tile == null || tile.IsValid == false)
                    continue;

                var centerX = tile.transform.localPosition.x;
                var width   = tile.Width;
                var half    = width * 0.5f;

                _baseCenters[dst] = centerX;
                _baseRights[dst]  = centerX + half;
                dst++;
            }

            _cycleWidth = _tileCount > 0 ? _baseRights[_tileCount - 1] : 0f;
        }

        [ContextMenu("처음 시작 배치")]
        public void AlignTiles() {
            if (_tiles == null || _tiles.Length == 0)
                return;

            var currentLeft = 0f;

            foreach (var tile in _tiles) {
                if (tile == null || tile.IsValid == false)
                    continue;

                var width = tile.Width;
                if (width <= 0f)
                    continue;

                var centerX = currentLeft + (width * 0.5f);
                tile.transform.SetLocalPositionX(centerX);

                currentLeft += width;
            }
        }

        private void NormalizeLayout() {
            if (_tiles == null || _tiles.Length == 0)
                return;

            var hasValid = false;
            var minLeft  = float.MaxValue;

            foreach (var tile in _tiles) {
                if (tile == null || tile.IsValid == false)
                    continue;

                var width = tile.Width;
                var left  = tile.transform.localPosition.x - (width * 0.5f);

                if (left < minLeft)
                    minLeft = left;

                hasValid = true;
            }

            if (hasValid == false)
                return;

            foreach (var tile in _tiles) {
                if (tile == null || tile.IsValid == false)
                    continue;

                var localPos = tile.transform.localPosition;
                localPos.x                   -= minLeft;
                tile.transform.localPosition =  localPos;
            }
        }

        private void SortTiles() {
            Array.Sort(_tiles, (left, right) => {
                var ax = left?.transform != null ? left.transform.localPosition.x : float.MaxValue;
                var bx = right?.transform != null ? right.transform.localPosition.x : float.MaxValue;
                return ax.CompareTo(bx);
            });
        }


        [ContextMenu("Validate Tiles")]
        public void ValidateTiles() {
            if (_tiles == null || _tiles.Length == 0) {
                Debug.LogWarning($"[ParallaxLayer] {name}: tiles is empty.");
                return;
            }

            for (int i = 0; i < _tiles.Length; i++) {
                var tile = _tiles[i];

                if (tile == null) {
                    Debug.LogWarning($"[ParallaxLayer] {name}: tile[{i}] is null.");
                    continue;
                }

                if (tile.transform == null) {
                    Debug.LogWarning($"[ParallaxLayer] {name}: tile[{i}] transform is null.");
                    continue;
                }

                if (tile.spriteRenderer == null) {
                    tile.spriteRenderer = tile.transform.GetComponent<SpriteRenderer>();
                }

                if (tile.spriteRenderer == null) {
                    Debug.LogWarning($"[ParallaxLayer] {name}: tile[{i}] has no SpriteRenderer.");
                }
            }

            Debug.Log($"[ParallaxLayer] {name}: validation complete.");
        }

        [ContextMenu("자동 레이어 등록")]
        public void CollectChildSprites() {
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);

            Array.Sort(renderers, (a, b) =>
                           a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

            _tiles = new Tile[renderers.Length];

            for (int i = 0; i < renderers.Length; i++) {
                _tiles[i] = new Tile {
                    transform      = renderers[i].transform,
                    spriteRenderer = renderers[i]
                };
            }

            Debug.Log($"[ParallaxLayer] {name}: collected {_tiles.Length} child sprites.");
        }
    }
}