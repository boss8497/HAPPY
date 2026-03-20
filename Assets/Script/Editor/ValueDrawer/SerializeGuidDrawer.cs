using System;
using Script.GameInfo;
using Script.GameInfo.Info;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.ValueDrawer {
    public class SerializeGuidDrawer : OdinValueDrawer<SerializeGuid> {
        protected override void DrawPropertyLayout(GUIContent label) {
            var value = ValueEntry.SmartValue;

            EditorGUILayout.BeginHorizontal();

            if (label != null)
                EditorGUILayout.PrefixLabel(label);

            using (new EditorGUI.DisabledScope(true)) {
                EditorGUILayout.TextField(value.Value.ToString("D"));
            }

            if (GUILayout.Button("Reset", GUILayout.Width(60))) {
                value                 = SerializeGuid.Empty();
                ValueEntry.SmartValue = value;
                ValueEntry.ApplyChanges();
                GUI.changed = true;
            }

            if (GUILayout.Button("New", GUILayout.Width(60))) {
                value                 = SerializeGuid.NewGuid();
                ValueEntry.SmartValue = value;
                ValueEntry.ApplyChanges();
                GUI.changed = true;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}