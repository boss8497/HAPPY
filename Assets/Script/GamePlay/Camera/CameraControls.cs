using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace Script.GamePlay.Camera {
    public class CameraControls : ICameraControls, IDisposable {
        private readonly Transform                          _transform;
        private readonly CinemachineBasicMultiChannelPerlin _noise;
        private readonly float                              _shakeMaxAmplitude;
        private readonly float                              _shakeDuration;
        private readonly float                              _shakeRecoveryDuration;

        public UnityEngine.Camera MainCamera { get; }

        public float OutSideLeftX => _transform.position.x - MainCamera.orthographicSize * MainCamera.aspect;
        public float InSideLeftX  => _transform.position.x + MainCamera.orthographicSize * MainCamera.aspect;
        public float SpawnOffset  => _transform.position.x + (MainCamera.orthographicSize * MainCamera.aspect * 2.0f);

        private float                    _currentAmplitude;
        private CancellationTokenSource _shakeCts;

        public CameraControls(
            UnityEngine.Camera mainCamera,
            CinemachineCamera  vCamera,
            float              cameraShakeMaxAmplitude,
            float              cameraShakeDuration,
            float              cameraShakeRecoveryDuration
        ) {
            MainCamera = mainCamera;
            _transform = MainCamera.transform;

            _shakeMaxAmplitude     = cameraShakeMaxAmplitude;
            _shakeDuration         = cameraShakeDuration;
            _shakeRecoveryDuration = cameraShakeRecoveryDuration;

            _noise = vCamera == null ? null : vCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (_noise == null) {
                Debug.LogError($"{nameof(CinemachineBasicMultiChannelPerlin)} 컴포넌트가 vCamera에 없습니다. Camera Shake가 동작하지 않습니다.");
            }
        }

        public void Shake(float amplitude) {
            if (amplitude <= 0f || _noise == null) return;

            _currentAmplitude    = Mathf.Min(_currentAmplitude + amplitude, _shakeMaxAmplitude);
            _noise.AmplitudeGain = _currentAmplitude;

            // 연속 요청 시 흔들리는 시간은 누적하지 않고 정해진 duration으로 매번 재시작한다.
            _shakeCts?.Cancel();
            _shakeCts?.Dispose();
            _shakeCts = new CancellationTokenSource();
            PlayAndRecoverAsync(_shakeCts.Token).Forget();
        }

        private async UniTaskVoid PlayAndRecoverAsync(CancellationToken ct) {
            var canceled = await UniTask.WaitForSeconds(_shakeDuration, cancellationToken: ct)
                                         .SuppressCancellationThrow();
            if (canceled) return;

            // 흔들림이 뚝 끊기지 않도록 recoveryDuration 동안 Amplitude를 0으로 서서히 감소시킨다.
            var startAmplitude = _currentAmplitude;
            var elapsed        = 0f;

            while (elapsed < _shakeRecoveryDuration) {
                elapsed += Time.deltaTime;

                var factor           = 1f - Mathf.Clamp01(elapsed / _shakeRecoveryDuration);
                _currentAmplitude    = startAmplitude * factor;
                _noise.AmplitudeGain = _currentAmplitude;

                canceled = await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow();
                if (canceled) return;
            }

            _currentAmplitude    = 0f;
            _noise.AmplitudeGain = 0f;
        }

        public void Dispose() {
            _shakeCts?.Cancel();
            _shakeCts?.Dispose();
            _shakeCts = null;
        }
    }
}
