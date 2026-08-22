using Script.GameInfo.Info;
using Script.GUI.ScreenData.Interface;
using Script.Tutorial;

namespace Script.GUI.ScreenData {
    public class FocusOption : IScreenOption {
        public TutorialFocusData TutorialFocusData { get; set; }
        public FocusGuide        FocusGuide        { get; set; }
    }
}