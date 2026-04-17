using Script.GameInfo.Attribute;
using Script.GameInfo.Info.Stat;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;

namespace Script.Editor.Attribute {
    
    public class StatusSelector : OdinSelector<StatusInfo> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            foreach (var statusInfo in GameInfoManager.Instance.GetCollection<StatusInfo>()) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{statusInfo.ID}({statusInfo.Name})", statusInfo));
            }
        }
    }
    
    
    public class StatusAttributeDrawer : InfoBaseAttribute<StatusAttribute, StatusInfo, StatusSelector>{
        protected override string GetName(StatusInfo value) {
            return value?.Name ?? "no name";
        }
        protected override int    GetUid(StatusInfo  value) {
            return value?.UID ?? -1;
        }
    }
}