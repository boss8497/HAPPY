using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Enum;
using Script.GameInfo.Item;

namespace Script.GameInfo {
    [AutoEditorTable(true)]
    [System.Serializable]
    public class RewardInfo : InfoBase {
        public RewardType   type;
        public ItemReward[] itemRewards;
    }
}