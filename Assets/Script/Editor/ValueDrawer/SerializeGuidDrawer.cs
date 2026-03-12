using System;
using Script.GameInfo.Info;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.ValueDrawer {
    public class SerializeGuidDrawer : OdinValueDrawer<SerializeGuid> {
        protected override void DrawPropertyLayout(GUIContent label) {
            var value = ValueEntry.SmartValue;

            // null 방어
            if (value == null) {
                EditorGUILayout.BeginHorizontal();

                SirenixEditorGUI.ErrorMessageBox("Guid wrapper is null.");

                if (GUILayout.Button("New", GUILayout.Width(60))) {
                    ValueEntry.SmartValue = new(Guid.NewGuid());
                }

                EditorGUILayout.EndHorizontal();
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // 라벨
            if (label != null) {
                EditorGUILayout.PrefixLabel(label);
            }

            // 읽기 전용 문자열 표시
            using (new EditorGUI.DisabledScope(true)) {
                EditorGUILayout.TextField(value.Value.ToString("D"));
            }

            // Reset 버튼
            if (GUILayout.Button("Reset", GUILayout.Width(60))) {
                value.Value           = Guid.Empty;
                ValueEntry.SmartValue = value;
            }

            // New 버튼
            if (GUILayout.Button("New", GUILayout.Width(60))) {
                value.Value           = Guid.NewGuid();
                ValueEntry.SmartValue = value;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}