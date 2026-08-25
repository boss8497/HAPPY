using R3;
using Script.GameData.Model;
using Script.GameInfo.Info;

namespace Script.GameData.Data.Interface {
    public interface IGroupData : IData<GroupModel> {
        ReadOnlyReactiveProperty<TutorialProgress> TutorialProgress { get; }
        bool                                       CanPlayTutorial(TutorialProgress progress);
    }
}