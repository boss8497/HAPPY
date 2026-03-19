namespace Script.GamePlay.Camera {
    public class CameraControls : ICameraControls {
        public UnityEngine.Camera MainCamera { get; private set; }

        public CameraControls(UnityEngine.Camera mainCamera) {
            MainCamera = mainCamera;
        }
    }
}