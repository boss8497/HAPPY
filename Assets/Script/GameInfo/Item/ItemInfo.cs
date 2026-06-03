using System;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Enum;
using Sirenix.OdinInspector;

namespace Script.GameInfo.Item {
    [AutoEditorTable(true)]
    [System.Serializable]
    public class ItemInfo : InfoBase {
        public ItemType type = ItemType.None;
        public ItemFlag flag = ItemFlag.None;

        [ShowIf("@type != Script.GameInfo.Enum.ItemType.Character"), Status]
        public int[] statusUids = Array.Empty<int>();
        
        [ShowIf("@type == Script.GameInfo.Enum.ItemType.Character || type == Script.GameInfo.Enum.ItemType.Obstacle"), Character]
        public int characterInfoUid;

        [Exp]
        public int[] expUids;
    }
}