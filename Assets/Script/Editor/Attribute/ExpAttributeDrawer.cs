using Script.GameInfo;
using Script.GameInfo.Attribute;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;

namespace Script.Editor.Attribute {
    public class ExpSelector : OdinSelector<ExpInfo> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            foreach (var itemInfo in GameInfoManager.Instance.GetCollection<ExpInfo>()) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{itemInfo.ID}({itemInfo.Name})", itemInfo));
            }
        }
    }


    public class ExpInfoAttributeDrawer : InfoBaseAttribute<ExpAttribute, ExpInfo, ExpSelector> {
        protected override string GetName(ExpInfo value) {
            return $"{value.ID}({value.Name})";
        }

        protected override int GetUid(ExpInfo value) {
            return value?.UID ?? 0;
        }
    }
}