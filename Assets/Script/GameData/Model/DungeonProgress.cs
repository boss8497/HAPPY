using System;
using MessagePack;

namespace Script.GameData.Model {
    [MessagePackObject]
    public class DungeonProgress {
        [Key(0)] public int  dungeonUid;
        [Key(1)] public Guid stageGuid;
        [Key(3)] public bool cleared;
    }
}