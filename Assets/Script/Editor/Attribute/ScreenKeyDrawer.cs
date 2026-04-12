using System;
using System.Linq;
using Script.GameInfo.Attribute;
using Script.GameInfo.Character;
using Script.GameInfo.Table;
using Script.GUI.Screen;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Script.Editor.Attribute {
    public class ScreenKeySelector : OdinSelector<string> {
        private readonly string _screenDataPath = nameof(ScreenData);

        protected override void BuildSelectionTree(OdinMenuTree tree) {
            tree.Config.DrawSearchToolbar             = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            tree.Config.SelectMenuItemsOnMouseDown    = true;
            tree.Selection.SupportsMultiSelect        = false;

            var handle = Addressables.LoadAssetAsync<ScreenData>(_screenDataPath);
            handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Load failed: {_screenDataPath}");

            tree.MenuItems.Add(new(tree, "None", null));
            tree.MenuItems.AddRange(handle.Result.screens.Select(i => new OdinMenuItem(tree, i.id, i.id)));
            Addressables.Release(handle);
        }
    }
    //
    // public class ScreenKeyDrawer {
    //     
    // }


    public class ScreenKeyDrawer : OdinAttributeDrawer<ScreenKeyAttribute, string> {
        private string _selected;

        protected override void Initialize() {
            _selected = ValueEntry.SmartValue ?? "None";
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            // Prefix 라벨
            if (label != null)
                EditorGUILayout.PrefixLabel(label);

            var btnRect = EditorGUILayout.GetControlRect();

            var currentName = (_selected ?? string.Empty);
            if (string.IsNullOrWhiteSpace(currentName))
                currentName = "None";

            if (UnityEngine.GUI.Button(btnRect, currentName)) {
                var selector = new ScreenKeySelector();
                selector.SelectionConfirmed += selection => {
                    var pick = selection.FirstOrDefault();
                    _selected = pick;

                    // 요소/단일 필드 모두 동일하게 string 값을 갱신
                    ValueEntry.SmartValue = string.IsNullOrEmpty(_selected) ? null : _selected;

                    // 값 변경 알림(필요시)
                    //Property.Tree?.NotifyValueChanged(Property);
                };
                selector.ShowInPopup(btnRect);
            }
        }
    }
}