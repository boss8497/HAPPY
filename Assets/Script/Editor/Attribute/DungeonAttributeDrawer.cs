using Script.GameInfo.Attribute;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;

namespace Script.Editor.Attribute {
    public class DungeonSelector : OdinSelector<DungeonInfo> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            foreach (var dungeonInfo in GameInfoManager.Instance.GetCollection<DungeonInfo>()) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{dungeonInfo.ID}", dungeonInfo));
            }
        }
    }

    public class DungeonAttributeDrawer : InfoBaseAttribute<DungeonAttribute, DungeonInfo, DungeonSelector> {
        protected override string GetName(DungeonInfo value) {
            return value.ID;
        }

        protected override int GetUid(DungeonInfo value) {
            return value?.UID ?? 0;
        }
    }
}