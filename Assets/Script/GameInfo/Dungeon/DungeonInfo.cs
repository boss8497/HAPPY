using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using System;
using System.Linq;
using Script.Utility.Runtime;

namespace Script.GameInfo.Dungeon {
    [AutoEditorTable(true)]
    [Serializable]
    public class DungeonInfo : InfoBase {
        public Category category;

        [Dungeon]
        public int nextDungeonUid;

        public Stage[] stages = Array.Empty<Stage>();


        public bool IsLastDungeon() {
            return !ValidUid(nextDungeonUid);
        }

        public bool IsLastStage(Stage stage) {
            return stage != null && stage == stages?.Last();
        }

        public bool IsLastStage(SerializeGuid guid) {
            return IsLastStage(stages?.FirstOrDefault(r => r.guid == guid));
        }

        public Stage NextStage(SerializeGuid stageGuid) {
            var index = stages.FindIndex(r => r.guid == stageGuid);
            return IsLastStage(stageGuid) ? stages[index] : stages[++index];
        }

        public Stage NextStage(Stage stage) {
            return stage == null ? null : NextStage(stage.guid);
        }
    }
}