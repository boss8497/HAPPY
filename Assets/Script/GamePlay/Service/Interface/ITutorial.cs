using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;

namespace Script.GamePlay.Service.Interface {
    public interface ITutorial : IService {
        UniTask StopAsync(bool       hide,  CancellationToken ct         = default);
        UniTask StartAsync(GuideBase guide, Action            onComplete = null, Action onSkip = null, CancellationToken ct = default);
    }
}