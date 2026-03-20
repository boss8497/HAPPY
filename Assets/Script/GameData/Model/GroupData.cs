using System;
using MessagePack;

namespace Script.GameData.Model {
    [MessagePackObject]
    public partial class GroupData {
        [Key(0)] public long uid;
        [Key(1)] public DungeonProgress[] dungeonProgresses = Array.Empty<DungeonProgress>();
    }
}