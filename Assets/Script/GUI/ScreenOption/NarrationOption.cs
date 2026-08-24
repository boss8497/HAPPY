using System;
using Script.GameInfo.Info;
using Script.GUI.ScreenData.Interface;

namespace Script.GUI.ScreenData {
    public class NarrationOption : IScreenOption {
        public NarrationGuide NarrationGuide   { get; set; }
        public Action         CompleteCallBack { get; set; }
    }
}