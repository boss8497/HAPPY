using Script.GameInfo.Attribute;
using Script.GameInfo.Info;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;

namespace Script.Editor.Attribute {
    public class TutorialSelector : OdinSelector<TutorialInfo> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            foreach (var itemInfo in GameInfoManager.Instance.GetCollection<TutorialInfo>()) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{itemInfo.ID}({itemInfo.Name})", itemInfo));
            }
        }
    }
    
    public class TutorialAttributeDrawer: InfoBaseAttribute<TutorialAttribute, TutorialInfo, TutorialSelector> {
        
        protected override string GetName(TutorialInfo value) {
            return $"{value.ID}({value.Name})";
        }

        protected override int GetUid(TutorialInfo value) {
            return value?.UID ?? 0;
        }
    }
}