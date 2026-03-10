using System;
using System.Linq;
using Script.GameInfo.Attribute;
using Script.GameInfo.Info.Character.Behaviour;
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
    
    
    public class BehaviourAttributeDrawer : OdinAttributeDrawer<BehaviourAttribute>{
        private int UID {
            get {
                if (Property.ValueEntry == null) {
                    return -1;
                }

                if (Property.ValueEntry.TypeOfValue == typeof(int)) {
                    return (int)Property.ValueEntry.WeakSmartValue;
                }

                return -1;
            }
            set {
                if (Property.ValueEntry.TypeOfValue == typeof(int)) {
                    Property.ValueEntry.WeakSmartValue = value;
                }
            }
        }
        
        private BehaviourInfo _info;
        
        protected override void Initialize() {
            if (UID != -1) {
                _info = GameInfoManager.Instance.Get<BehaviourInfo>(UID);
            }
        }

        public override bool CanDrawTypeFilter(Type type) {
            return type == typeof(int);
        }
        
        protected override void DrawPropertyLayout(GUIContent label) {
            
            EditorGUILayout.BeginHorizontal();
            if (label != null)
                EditorGUILayout.PrefixLabel(label);

            var name = _info == null ? "None" : GetName(_info);
            var rect = EditorGUILayout.GetControlRect();
            if (GUI.Button(rect, name)) {
                var selector = new BehaviourSelector();
                selector.SelectionConfirmed += selection => {
                    _info = selection.FirstOrDefault();
                    UID   = _info != null ? GetUID(_info) : -1;
                };
                selector.ShowInPopup(rect);
            }

            if (GUILayout.Button("Reset", GUILayout.Width(50))) {
                _info = null;
                UID  = -1;
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private string GetName(BehaviourInfo value) {
            return value.ID;
        }

        private int GetUID(BehaviourInfo value) {
            return value?.UID ?? -1;
        }
    }
}