using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Script.GUI.Screen {
    /// <summary>
    /// 스테이지 시작/재시작 시 맵/구조물 스폰, 캐릭터 T포즈 등 준비 과정을 가리기 위한 전환 오버레이.
    /// StageTransition Layer(CreateLayer에서 AddComponent로 생성)의 RawImage/CanvasGroup을 직접 제어한다.
    /// </summary>
    public partial class ScreenManager {
        [SerializeField]
        private float stageTransitionFadeDuration = 0.25f;

        private RawImage    _stageTransitionImage;
        private CanvasGroup _stageTransitionCanvasGroup;

        // ※ ScreenManagerDebugWindow.cs가 리플렉션으로 참조함 — 필드명은 아래 FieldName 상수(nameof)로만 참조할 것
        public const string StageTransitionSnapshotFieldName = nameof(_stageTransitionSnapshot);

        private Texture2D _stageTransitionSnapshot;

        private CancellationTokenSource _stageTransitionFadeCts;

        private void SetupStageTransitionLayer(GameObject layerObject) {
            _stageTransitionImage       = layerObject.AddComponent<RawImage>();
            _stageTransitionCanvasGroup = layerObject.AddComponent<CanvasGroup>();

            _stageTransitionCanvasGroup.alpha          = 0f;
            _stageTransitionCanvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 현재 화면을 캡처해 StageTransition Layer로 덮는다.
        /// 스냅샷이 이미 있으면(직전 HideStageTransitionAsync가 페이드 도중 취소된 경우 포함) 재캡처하지 않는다.
        /// </summary>
        public async UniTask ShowStageTransitionAsync() {
            CancelStageTransitionFade();

            if (_stageTransitionSnapshot == null) {
                // CaptureScreenshotAsTexture()는 해당 프레임의 렌더링이 끝난 뒤(WaitForEndOfFrame 이후)에
                // 호출해야 유효한 픽셀을 반환한다. 그 전에 호출하면 빈/무효 텍스처가 나온다.
                await UniTask.WaitForEndOfFrame(this);

                _stageTransitionSnapshot      = ScreenCapture.CaptureScreenshotAsTexture();
                _stageTransitionImage.texture = _stageTransitionSnapshot;
            }

            _stageTransitionCanvasGroup.alpha          = 1f;
            _stageTransitionCanvasGroup.blocksRaycasts = true;

            await ShowLoadingAsync();
        }

        /// <summary>
        /// 덮어둔 화면을 Fade Out으로 걷어내고 캡처해둔 텍스처를 해제한다. 덮여 있지 않으면 아무 것도 하지 않는다.
        /// </summary>
        public async UniTask HideStageTransitionAsync() {
            if (_stageTransitionSnapshot == null) return;

            await HideLoadingAsync();

            CancelStageTransitionFade();
            _stageTransitionFadeCts = new CancellationTokenSource();
            var ct = _stageTransitionFadeCts.Token;

            var elapsed = 0f;
            while (elapsed < stageTransitionFadeDuration) {
                elapsed                           += Time.unscaledDeltaTime;
                _stageTransitionCanvasGroup.alpha =  1f - Mathf.Clamp01(elapsed / stageTransitionFadeDuration);

                var canceled = await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct)
                                             .SuppressCancellationThrow();
                // ShowStageTransitionAsync가 페이드 도중 다시 호출된 경우 - 알파는 이미 1로 복구되어 있으니 그대로 반환
                if (canceled) return;
            }

            _stageTransitionCanvasGroup.alpha          = 0f;
            _stageTransitionCanvasGroup.blocksRaycasts = false;
            ReleaseStageTransitionSnapshot();
        }

        private void CancelStageTransitionFade() {
            if (_stageTransitionFadeCts == null) return;
            _stageTransitionFadeCts.Cancel();
            _stageTransitionFadeCts.Dispose();
            _stageTransitionFadeCts = null;
        }

        private void ReleaseStageTransitionSnapshot() {
            if (_stageTransitionSnapshot == null) return;
            Destroy(_stageTransitionSnapshot);
            _stageTransitionSnapshot = null;
        }

        private void OnDestroy() {
            CancelStageTransitionFade();
            ReleaseStageTransitionSnapshot();
        }
    }
}
