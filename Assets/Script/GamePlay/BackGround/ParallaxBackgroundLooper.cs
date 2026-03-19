using UnityEngine;

namespace Script.GamePlay.Background {
    [DisallowMultipleComponent]
    public class ParallaxBackgroundLooper : MonoBehaviour {
        [Header("References")]
        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private Transform _target;

        [Header("Layers")]
        [SerializeField]
        private ParallaxLayer[] _layers;

        private float _startTargetX;
        private bool  _initialized;

        private void Awake() {
            Initialize();
        }

        private void LateUpdate() {
            if (_initialized == false)
                return;

            if (_camera == null || _target == null || _layers == null)
                return;

            var targetDeltaX = _target.position.x - _startTargetX;
            var cameraLeftX  = GetCameraLeftX();

            foreach (var layer in _layers) {
                if (layer == null)
                    continue;

                layer.Tick(targetDeltaX, cameraLeftX);
            }
        }

        [ContextMenu("Initialize")]
        public void Initialize() {
            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null) {
                Debug.LogError("[ParallaxBackgroundLooper] Camera is null.");
                return;
            }

            if (_target == null) {
                _target = _camera.transform;
            }

            _startTargetX = _target.position.x;

            if (_layers != null) {
                foreach (var layer in _layers) {
                    if (layer == null)
                        continue;
                    layer.Initialize();
                }
            }

            _initialized = true;
        }

        private float GetCameraLeftX() {
            return _camera.transform.position.x - (_camera.orthographicSize * _camera.aspect);
        }
    }
}