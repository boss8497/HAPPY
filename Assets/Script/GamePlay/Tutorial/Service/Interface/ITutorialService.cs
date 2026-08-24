using Script.GameInfo.Info;

namespace Script.GamePlay.Service.Interface {
    public interface ITutorialService : IService {
        void StartTutorial(TutorialInfo tutorialInfo);
        void StartTutorial(int          uid);
        void StopTutorial();
    }
}