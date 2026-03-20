using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using System;

namespace Script.GameInfo.Dungeon {
    [AutoEditorTable(true)]
    [Serializable]
    public class DungeonInfo : InfoBase {
        public Stage[] stages = Array.Empty<Stage>();
    }
}