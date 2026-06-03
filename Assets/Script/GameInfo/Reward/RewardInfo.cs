using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Enum;
using Script.GameInfo.Info.Enum;
using Script.GameInfo.Item;
using Sirenix.OdinInspector;

namespace Script.GameInfo {
    [AutoEditorTable(true)]
    [System.Serializable]
    public class RewardInfo : InfoBase {
        public RewardType   type;
        [ShowIf("@type == RewardType.CharacterExp || type == RewardType.Exp")]
        public LevelType    expLevelType;
        public ItemReward[] itemRewards;
    }
}