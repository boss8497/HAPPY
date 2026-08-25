using System;
using MessagePack;
using Script.GameInfo.Info;

namespace Script.GameData.Model {
    [MessagePackObject]
    public partial class GroupModel {
        [Key(0)] public long              uid;
        [Key(1)] public DungeonProgress[] dungeonProgresses = Array.Empty<DungeonProgress>();
        [Key(2)] public TutorialProgress  tutorialProgress  = TutorialProgress.None;
    }


    [MessagePackObject]
    public class DungeonProgress {
        [Key(0)] public int  dungeonUid;
        [Key(1)] public Guid stageGuid;
        [Key(3)] public bool cleared;
        [Key(4)] public int  category;
    }
}