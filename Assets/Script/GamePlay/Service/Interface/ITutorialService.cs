using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.Tutorial;

namespace Script.GamePlay.Service.Interface {
    public interface ITutorialService : IService {
        public bool BlockButton { get; set; }

        UniTask StartFocusAsync(GuideBase guide, Action            onComplete = null, Action onSkip = null, CancellationToken ct = default);
        UniTask StopFocusAsync(bool       hide,  CancellationToken ct         = default);


        void RegisterFocusData(TutorialFocusData   data);
        void UnRegisterFocusData(TutorialFocusData data);


        TutorialFocusData GetFocusData(GuideBase guide);
    }
}