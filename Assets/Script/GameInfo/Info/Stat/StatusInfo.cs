using System;
using Script.GameInfo.Base;
using Script.GameInfo.Info.Enum;

namespace Script.GameInfo.Info.Stat {
    [System.Serializable]
    public class StatusInfo : InfoBase {
        public LevelType levelType;
        public Stat[]    status = Array.Empty<Stat>();
    }
}