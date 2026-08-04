using System.Collections.Generic;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Sirenix.Serialization;

namespace Script.GameInfo.Info {
    [AutoEditorTable(true)]
    [System.Serializable]
    public class TutorialInfo : InfoBase {
        [OdinSerialize]
        public List<GuideBase> sets = new List<GuideBase>();

        public bool systemControl;
        public bool allCloseScreen;
    }
}