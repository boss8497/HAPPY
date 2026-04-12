using Cysharp.Threading.Tasks;
using Script.GamePlay.Camera;
using Script.GamePlay.Stage;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Script.GamePlay.Background {
    [DisallowMultipleComponent]
    public class ParallaxBackgroundLooper : MonoBehaviour {
        private UnityEngine.Camera _camera;
        private Transform          _target;

        [Header("Layers")]
        [SerializeField]
        private ParallaxLayer[] _layers;

        private bool _initialized;

        private IStageManager   _stageManager;
        private ICameraControls _cameraControls;

        [Inject]
        public void Constructor(
            IStageManager   stageManager,
            ICameraControls cameraControls
        ) {
            _stageManager   = stageManager;
            _cameraControls = cameraControls;
        }

        private void Awake() {
            Initialize().Forget();
        }

        private void LateUpdate() {
            if (_initialized == false || (_stageManager?.SystemControl?.CurrentValue ?? true))
                return;

            var targetPos   = _target.position;
            var cameraLeftX = GetCameraLeftX();

            if (_layers == null)
                return;

            foreach (var layer in _layers) {
                if (layer == null)
                    continue;

                layer.Tick(targetPos, cameraLeftX);
            }
        }

        private async UniTask Initialize() {
            await UniTask.WaitUntil(() =>
                _cameraControls != null &&
                _cameraControls.MainCamera != null);

            _camera = _cameraControls.MainCamera;
            if (_camera == null) {
                Debug.LogError("[ParallaxBackgroundLooper] MainCamera is null.");
                return;
            }

            // 필요 시 플레이어 Transform 으로 교체 가능
            _target = _camera.transform;

            var targetPos = _target.position;

            if (_layers != null) {
                foreach (var layer in _layers) {
                    if (layer == null)
                        continue;

                    layer.Initialize(targetPos);
                }
            }

            _initialized = true;
        }

        private float GetCameraLeftX() {
            return _camera.transform.position.x - (_camera.orthographicSize * _camera.aspect);
        }
    }
}