using Script.GameInfo.Attribute;
using Script.GameInfo.Item;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;

namespace Script.Editor.Attribute {
    
    public class ItemSelector : OdinSelector<ItemInfo> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            foreach (var itemInfo in GameInfoManager.Instance.GetCollection<ItemInfo>()) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{itemInfo.ID}", itemInfo));
            }
        }
    }


    public class ItemAttributeDrawer : InfoBaseAttribute<ItemAttribute, ItemInfo, ItemSelector> {
        protected override string GetName(ItemInfo value) {
            return value.ID;
        }

        protected override int GetUid(ItemInfo value) {
            return value?.UID ?? 0;
        }
    }
}