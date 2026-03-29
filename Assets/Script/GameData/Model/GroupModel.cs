using System;
using MessagePack;

namespace Script.GameData.Model {
    [MessagePackObject]
    public partial class GroupModel {
        [Key(0)] public long uid;
        [Key(1)] public DungeonProgressModel[] dungeonProgresses = Array.Empty<DungeonProgressModel>();
    }
}