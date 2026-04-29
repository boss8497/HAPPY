using UnityEngine;

namespace Script.GamePlay.Camera {
    public class CameraControls : ICameraControls {
        private readonly Transform _transform;
        public UnityEngine.Camera MainCamera { get; private set; }

        public float OutSideLeftX => _transform.position.x - MainCamera.orthographicSize * MainCamera.aspect;

        public CameraControls(UnityEngine.Camera mainCamera) {
            MainCamera = mainCamera;
            _transform = MainCamera.transform;
        }
        
    }
}