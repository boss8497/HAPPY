using System;
using System.Linq;
using Script.GameInfo.Base;
using Script.GameInfo.Table;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.Attribute {
    public abstract class InfoBaseAttribute <TAttribute, TInfo, TSelector> : OdinAttributeDrawer<TAttribute>
        where TAttribute : System.Attribute
        where TInfo : InfoBase
        where TSelector : OdinSelector<TInfo>, new() {
        protected int UID {
            get {
                if (Property.ValueEntry == null) {
                    return 0;
                }

                if (Property.ValueEntry.TypeOfValue == typeof(int)) {
                    return (int)Property.ValueEntry.WeakSmartValue;
                }

                return 0;
            }
            set {
                if (Property.ValueEntry.TypeOfValue == typeof(int)) {
                    Property.ValueEntry.WeakSmartValue = value;
                }
            }
        }

        protected TInfo Info;


        protected override void Initialize() {
            if (UID > 0) {
                Info = GameInfoManager.Instance.Get<TInfo>(UID);
            }
        }


        protected override void DrawPropertyLayout(GUIContent label) {
            EditorGUILayout.BeginHorizontal();
            if (label != null)
                EditorGUILayout.PrefixLabel(label);

            var name = Info == null ? "None" : GetName(Info);
            var rect = EditorGUILayout.GetControlRect();
            if (GUI.Button(rect, name)) {
                var selector = new TSelector();
                selector.SelectionConfirmed += selection => {
                    Info = selection.FirstOrDefault();
                    UID   = Info != null ? GetUid(Info) : 0;
                };
                selector.ShowInPopup(rect);
            }

            if (GUILayout.Button("Reset", GUILayout.Width(50))) {
                Info = null;
                UID   = 0;
            }

            EditorGUILayout.EndHorizontal();
        }

        public override bool CanDrawTypeFilter(Type type) {
            return type == typeof(int);
        }

        protected abstract string GetName(TInfo value);

        protected abstract int GetUid(TInfo value);
    }
}