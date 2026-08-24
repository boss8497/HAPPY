using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.GamePlay.Service.Interface;

namespace Script.GamePlay.Service {
    public class NarrationService : INarrationService {
        private const string NarrationScreenKey = "TutorialNarration";
        
        public        bool   Initialized { get; private set; }

        public UniTask StopAsync(bool hide, CancellationToken ct = default) {
            throw new NotImplementedException();
        }

        public UniTask StartAsync(GuideBase guide, Action onComplete = null, Action onSkip = null, CancellationToken ct = default) {
            throw new NotImplementedException();
        }
    }
}