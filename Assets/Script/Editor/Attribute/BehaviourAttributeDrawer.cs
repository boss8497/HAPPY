using System;
using System.Linq;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Character;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.Attribute {
    public class BehaviourSelector : OdinSelector<BehaviourInfo> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            foreach (var behaviour in GameInfoManager.Instance.GetCollection<BehaviourInfo>()) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{behaviour.ID}", behaviour));
            }
        }
    }

    public class BehaviourAttributeDrawer : InfoBaseAttribute<BehaviourAttribute, BehaviourInfo,BehaviourSelector> {
        protected override string GetName(BehaviourInfo value) {
            return value.ID;
        }

        protected override int GetUid(BehaviourInfo value) {
            return value?.UID ?? 0;
        }
    }
}