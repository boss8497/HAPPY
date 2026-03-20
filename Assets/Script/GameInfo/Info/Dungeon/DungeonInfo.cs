using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using System;

namespace Script.GameInfo.Info.Dungeon {
    [AutoEditorTable(true)]
    [Serializable]
    public class DungeonInfo : InfoBase {
        public Stage[] stages;
    }
}