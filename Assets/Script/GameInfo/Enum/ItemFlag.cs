using System;

namespace Script.GameInfo.Enum {
    [Flags]
    public enum ItemFlag {
        None,
        Stack,            // 아이템 겹치기가 가능한지?
        IgnoreGradeEqual, // 아이템 비교할 때 Grade 비교할건지
        IgnoreLevelEqual, // 아이템 비교할 때 Level 비교할건지
        IgnoreTierEqual,  // 아이템 비교할 때 Tier 비교할건지
    }
}