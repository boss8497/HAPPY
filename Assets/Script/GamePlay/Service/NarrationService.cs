using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.GamePlay.Service.Interface;
using Script.GUI.Screen.Interface;
using Script.GUI.ScreenData;
using Script.Tutorial.Interface;

namespace Script.GamePlay.Service {
    public class NarrationService : INarrationService {
        private const    string         NarrationScreenKey = "TutorialNarration";
        private readonly IScreenManager _screenManager;

        private ITutorialScreen _narrationScreen;

        public bool Initialized { get; private set; }

        public NarrationService(IScreenManager screenManager) {
            _screenManager = screenManager;
            Initialized    = true;
        }

        public async UniTask StopAsync(bool hide, CancellationToken ct = default) {
            if (_narrationScreen == null) return;
            await _narrationScreen.StopAsync(hide, ct);
        }

        public async UniTask StartAsync(GuideBase guide, Action onComplete = null, Action onSkip = null, CancellationToken ct = default) {
            await StopAsync(false, ct);

            if (guide is NarrationGuide narrationGuide) {
                _narrationScreen = await _screenManager.OpenAsync(new NarrationOption() {
                                                                      NarrationGuide   = narrationGuide,
                                                                      CompleteCallBack = onComplete
                                                                  }, NarrationScreenKey, ct) as ITutorialScreen;
            }
            else {
                onComplete?.Invoke();
            }
        }

        public void Dispose() {
            // TODO release managed resources here
        }
    }
}