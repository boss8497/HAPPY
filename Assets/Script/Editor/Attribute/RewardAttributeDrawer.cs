using Script.GameInfo;
using Script.GameInfo.Attribute;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;

namespace Script.Editor.Attribute {
    public class RewardSelector : OdinSelector<RewardInfo> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            foreach (var itemInfo in GameInfoManager.Instance.GetCollection<RewardInfo>()) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{itemInfo.ID}({itemInfo.Name})", itemInfo));
            }
        }
    }


    public class RewardAttributeDrawer : InfoBaseAttribute<RewardAttribute, RewardInfo, RewardSelector> {
        protected override string GetName(RewardInfo value) {
            return $"{value.ID}({value.Name})";
        }

        protected override int GetUid(RewardInfo value) {
            return value?.UID ?? 0;
        }
    }
}