using Script.GameInfo.Attribute;
using Script.GameInfo.Info;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;

namespace Script.Editor.Attribute {
    
    public class BuffSelector : OdinSelector<BuffInfo> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            foreach (var buff in GameInfoManager.Instance.GetCollection<BuffInfo>()) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{buff.ID}({buff.Name})", buff));
            }
        }
    }

    
    public class BuffAttributeDrawer  : InfoBaseAttribute<BuffAttribute, BuffInfo,BuffSelector> {
        protected override string GetName(BuffInfo value) {
            return value.ID;
        }

        protected override int GetUid(BuffInfo value) {
            return value?.UID ?? 0;
        }
    }
}