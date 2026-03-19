using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.GamePlay.Background {
    [DisallowMultipleComponent]
    public class ParallaxLayer : MonoBehaviour {
        [Serializable]
        public class Tile {
            public Transform      transform;
            public SpriteRenderer spriteRenderer;

            public float Width  => spriteRenderer != null ? spriteRenderer.bounds.size.x : 0f;
            public float LeftX  => spriteRenderer != null ? spriteRenderer.bounds.min.x : transform.position.x;
            public float RightX => spriteRenderer != null ? spriteRenderer.bounds.max.x : transform.position.x;

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

        private Queue<Tile> _queue;
        private float       _startRootX;
        private float       _startY;
        private float       _startZ;
        private bool        _initialized;

        public float ParallaxFactor => _parallaxFactor;
        public bool  LoopEnabled    => _loop;
        public bool  IsInitialized  => _initialized;

        public void Initialize() {
            _startRootX = transform.position.x + _startXOffset;
            _startY     = transform.position.y;
            _startZ     = transform.position.z;

            var startPos = transform.position;
            startPos.x         = _startRootX;
            transform.position = startPos;

            _queue = new Queue<Tile>(_tiles != null ? _tiles.Length : 0);

            if (_tiles == null || _tiles.Length == 0) {
                _initialized = true;
                return;
            }

            foreach (var tile in _tiles) {
                if (tile == null || tile.transform == null)
                    continue;

                if (tile.spriteRenderer == null)
                    tile.spriteRenderer = tile.transform.GetComponent<SpriteRenderer>();
            }

            if (_autoAlignOnStart) {
                AlignTilesLeftToRight();
            }

            foreach (var tile in _tiles) {
                if (tile == null || tile.IsValid == false)
                    continue;

                if (tile.transform.parent != transform)
                    tile.transform.SetParent(transform, true);

                _queue.Enqueue(tile);
            }

            _initialized = true;
        }

        public void Tick(float targetDeltaX, float cameraLeftX) {
            if (_initialized == false)
                return;

            UpdateParallax(targetDeltaX);

            if (_loop) {
                UpdateLoop(cameraLeftX);
            }
        }

        private void UpdateParallax(float targetDeltaX) {
            var pos = transform.position;
            pos.x              = _startRootX + (targetDeltaX * _parallaxFactor);
            pos.y              = _startY;
            pos.z              = _startZ;
            transform.position = pos;
        }

        private void UpdateLoop(float cameraLeftX) {
            if (_queue == null || _queue.Count == 0)
                return;

            var safety = 0;

            while (_queue.Count > 0) {
                var front = _queue.Peek();
                if (front == null || front.IsValid == false)
                    break;

                var recycleX = cameraLeftX + _cameraLeftOffset - _recycleOffset;
                if (front.RightX >= recycleX)
                    break;

                _queue.Dequeue();

                var last = GetLastTile();
                if (last == null || last.IsValid == false) {
                    _queue.Enqueue(front);
                    break;
                }

                var newLeftX   = last.RightX;
                var newCenterX = newLeftX + (front.Width * 0.5f);

                var pos = front.transform.position;
                pos.x                    = newCenterX;
                front.transform.position = pos;

                _queue.Enqueue(front);

                safety++;
                if (safety > 100) {
                    Debug.LogWarning($"[ParallaxLayer] Safety break: {name}");
                    break;
                }
            }
        }

        private Tile GetLastTile() {
            Tile last = null;
            foreach (Tile tile in _queue) {
                last = tile;
            }

            return last;
        }

        [ContextMenu("처음 시작 배치")]
        public void AlignTilesLeftToRight() {
            if (_tiles == null || _tiles.Length == 0)
                return;

            var currentLeft = 0f;
            var firstPlaced = false;

            foreach (var tile in _tiles) {
                if (tile == null || tile.transform == null)
                    continue;

                if (tile.spriteRenderer == null)
                    tile.spriteRenderer = tile.transform.GetComponent<SpriteRenderer>();

                if (tile.IsValid == false)
                    continue;

                float width = tile.Width;
                if (width <= 0f)
                    continue;

                var pos = tile.transform.localPosition;

                if (firstPlaced == false) {
                    pos.x       = width * 0.5f;
                    firstPlaced = true;
                }
                else {
                    pos.x = currentLeft + (width * 0.5f);
                }

                tile.transform.localPosition = pos;
                currentLeft                  = tile.RightX - transform.position.x;
            }
        }

        [ContextMenu("Validate Tiles")]
        public void ValidateTiles() {
            if (_tiles == null || _tiles.Length == 0) {
                Debug.LogWarning($"[ParallaxLayer] {name}: tiles is empty.");
                return;
            }

            for (int i = 0; i < _tiles.Length; i++) {
                Tile tile = _tiles[i];
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