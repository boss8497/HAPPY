using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.Tutorial;

namespace Script.GamePlay.Service.Interface {
    public interface IFocusService : ITutorial {
        public bool BlockButton { get; set; }

        UniTask<TutorialFocusData> GetRetryFocusData(GuideBase guideData, int maxRetryCount = 100, CancellationToken ct = default);

        void RegisterFocusData(TutorialFocusData   data);
        void UnRegisterFocusData(TutorialFocusData data);


        TutorialFocusData GetFocusData(GuideBase guide);
    }
}