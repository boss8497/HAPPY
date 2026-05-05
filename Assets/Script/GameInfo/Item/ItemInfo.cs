using System;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Enum;

namespace Script.GameInfo.Item {
    [AutoEditorTable(true)]
    [System.Serializable]
    public class ItemInfo : InfoBase {
        public ItemType type = ItemType.None;
        public ItemFlag flag = ItemFlag.None;
        
        [Status]
        public int[] statusUids = Array.Empty<int>();
        
    }
}