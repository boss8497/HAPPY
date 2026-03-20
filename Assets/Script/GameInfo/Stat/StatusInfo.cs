using System;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Info.Enum;

namespace Script.GameInfo.Info.Stat {
    [AutoEditorTable(true)]
    [System.Serializable]
    public class StatusInfo : InfoBase {
        public LevelType levelType;
        public Stat[]    status = Array.Empty<Stat>();
    }
}