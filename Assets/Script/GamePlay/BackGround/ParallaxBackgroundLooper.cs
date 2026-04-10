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

        private float _startTargetX;
        private bool  _initialized;

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

            var targetDeltaX = _target.position.x - _startTargetX;
            var cameraLeftX  = GetCameraLeftX();

            foreach (var layer in _layers) {
                if (layer == null)
                    continue;

                layer.Tick(targetDeltaX, cameraLeftX);
            }
        }

        private async UniTaskVoid Initialize() {
            await UniTask.WaitUntil(() => _cameraControls != null);
            
            _camera = _cameraControls.MainCamera;
            _target = _camera.transform;
            
            if (_camera == null) {
                Debug.LogError("[ParallaxBackgroundLooper] MainCamera is null.");
                return;
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

        
        //TODO: Ui 만들어지 전까지 테스트 코드
        [Button("Restart")]
        public void TestRestart() {
            _stageManager.ReStart().Forget();
        }
    }
}