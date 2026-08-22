using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.Tutorial;
using Script.Tutorial.Interface;

namespace Script.GamePlay.Service.Interface {
    public interface ITutorialService : IService {
        public bool BlockButton { get; set; }


        void RegisterFocusData(TutorialFocusData   data);
        void UnRegisterFocusData(TutorialFocusData data);


        TutorialFocusData          GetFocusData(GuideBase      guide);
    }
}