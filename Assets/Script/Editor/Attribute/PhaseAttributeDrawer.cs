using Script.GameInfo.Attribute;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;

namespace Script.Editor.Attribute {
    public class PhaseSelector : OdinSelector<PhaseInfo> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            foreach (var info in GameInfoManager.Instance.GetCollection<PhaseInfo>()) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{info.ID}", info));
            }
        }
    }

    public class PhaseAttributeDrawer : InfoBaseAttribute<PhaseAttribute, PhaseInfo, PhaseSelector> {
        protected override string GetName(PhaseInfo value) {
            return value.ID;
        }

        protected override int GetUid(PhaseInfo value) {
            return value?.UID ?? 0;
        }
    }
}