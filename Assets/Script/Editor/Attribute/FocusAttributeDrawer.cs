using System;
using System.Collections.Generic;
using System.Linq;
using Script.GameInfo;
using Script.GameInfo.Attribute;
using Script.Tutorial;
using Script.Tutorial.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.Attribute {
    public class FocusSelector : OdinSelector<TutorialFocusData> {
        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            if (FocusAttributeDrawer.FocusDataList == null) {
                FocusAttributeDrawer.FocusDataList = TutorialFocusDataManager.GetFocusData();
            }

            foreach (var focusScript in FocusAttributeDrawer.FocusDataList) {
                tree.MenuItems.Add(new OdinMenuItem(tree, $"{focusScript.id}", focusScript));
            }
        }
    }

    public class FocusAttributeDrawer : OdinAttributeDrawer<FocusAttribute> {
        public static List<TutorialFocusData> FocusDataList;

        private SerializeGuid Guid {
            get {
                return Property.ValueEntry.TypeOfValue switch {
                    { } t when t == typeof(SerializeGuid) => (SerializeGuid)Property.ValueEntry.WeakSmartValue,
                    { } t when t == typeof(string) => Property.ValueEntry.WeakSmartValue is string guidString
                                                          ? string.IsNullOrEmpty(guidString)
                                                                ? SerializeGuid.Empty
                                                                : new SerializeGuid(System.Guid.Parse(guidString))
                                                          : SerializeGuid.Empty,
                    _ => SerializeGuid.Empty
                };
            }
            set {
                Property.ValueEntry.WeakSmartValue = Property.ValueEntry.TypeOfValue switch {
                    { } t when t == typeof(Guid)   => value,
                    { } t when t == typeof(string) => value.ToString(),
                    _                              => Property.ValueEntry.WeakSmartValue
                };

                Property.ValueEntry.ApplyChanges();
            }
        }

        private TutorialFocusData _obj;

        protected override void Initialize() {
            if (SerializeGuid.Empty != Guid) {
                _obj = TutorialFocusDataManager.GetFocusData().FirstOrDefault(r => r.Guid == Guid);
            }
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            EditorGUILayout.BeginHorizontal();
            if (label != null)
                EditorGUILayout.PrefixLabel(label);

            var name = _obj == null ? "None" : GetName(_obj);
            var rect = EditorGUILayout.GetControlRect();
            if (UnityEngine.GUI.Button(rect, name)) {
                var selector = new FocusSelector();
                selector.SelectionConfirmed += selection => {
                    _obj = selection.FirstOrDefault();
                    Guid = _obj != null ? GetGuid(_obj) : SerializeGuid.Empty;
                };
                selector.ShowInPopup(rect);
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(50))) {
                FocusAttributeDrawer.FocusDataList = TutorialFocusDataManager.GetFocusData();
            }

            EditorGUILayout.EndHorizontal();
        }

        private static string GetName(TutorialFocusData value) {
            return $"{value.id}({value.guid})";
        }

        private static SerializeGuid GetGuid(TutorialFocusData value) {
            return value?.Guid ?? SerializeGuid.Empty;
        }
    }
}