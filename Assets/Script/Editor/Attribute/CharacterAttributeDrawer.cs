using Script.GameInfo.Attribute;
using Script.GameInfo.Info.Character;
using Script.GameInfo.Info.Character.Behaviour;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;

namespace Script.Editor.Attribute {
    
    public class CharacterSelector : OdinSelector<CharacterInfo> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            foreach (var characterInfo in GameInfoManager.Instance.GetCollection<CharacterInfo>()) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{characterInfo.ID}", characterInfo));
            }
        }
    }
    
    
    public class CharacterAttributeDrawer : InfoBaseAttribute<CharacterAttribute, CharacterInfo, CharacterSelector> {
        protected override string GetName(CharacterInfo value) {
            return value.ID;
        }

        protected override int GetUid(CharacterInfo value) {
            return value?.UID ?? 0;
        }
    }
}