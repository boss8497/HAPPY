using System;
using System.Linq;
using Script.GameInfo.Attribute;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;


namespace Script.Editor.Attribute {
    public class AssetPathDrawer : OdinAttributeDrawer<AssetPathAttribute, string> {
        private Type _type;

        protected override void Initialize() {
            base.Initialize();

            _type = Attribute.Type switch {
                _ when Attribute.Type == typeof(UnityEngine.SceneManagement.Scene) => typeof(SceneAsset),
                _                                                                  => Attribute.Type
            };
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            EditorGUILayout.BeginHorizontal();
            if (label != null) {
                EditorGUILayout.PrefixLabel(label);
            }

            var path = ValueEntry.SmartValue;
            if (Attribute.Type == typeof(Sprite)) {
                ValueEntry.SmartValue = DrawSprite(path);
            }
            else {
                DrawAsset(path);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static string GetAssetPath(string key) {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (var group in settings.groups) {
                if (group == null) continue;
                foreach (var entry in group.entries) {
                    if (entry.address == key) {
                        return AssetDatabase.GUIDToAssetPath(entry.guid);
                    }
                }
            }

            return null;
        }

        private static string DrawSprite(string assetGuid) {
            if (string.IsNullOrEmpty(assetGuid)) {
                assetGuid = "";
            }

            Sprite sprite = null;

            if (!string.IsNullOrEmpty(assetGuid)) {
                var split = assetGuid.Split(':');
                if (split.Length <= 1) {
                    var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                    if (string.IsNullOrEmpty(assetPath)) {
                        assetPath = GetAssetPath(assetGuid);
                    }

                    if (!string.IsNullOrEmpty(assetPath)) {
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    }
                }
                else {
                    var assetPath = AssetDatabase.GUIDToAssetPath(split[0]);
                    sprite = AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(r => r is Sprite).Cast<Sprite>().FirstOrDefault(r => r.name == split[1]);
                }
            }

            EditorGUI.BeginChangeCheck();
            var newSprite = (Sprite)EditorGUILayout.ObjectField(sprite, typeof(Sprite), false, GUILayout.Width(200), GUILayout.Height(200));
            if (!EditorGUI.EndChangeCheck())
                return assetGuid;

            sprite = newSprite;
            var newPath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(newPath)) {
                return null;
            }

            var spriteAssets = AssetDatabase.LoadAllAssetsAtPath(newPath).Where(r => r is Sprite).Cast<Sprite>().ToList();
            var sliceFlag    = spriteAssets.Count > 1;

            var guid = AssetDatabase.AssetPathToGUID(newPath);
            if (!string.IsNullOrEmpty(guid)) {
                return sliceFlag ? string.Format($"{guid}:{newSprite.name}") : guid;
            }

            // Addressable에 등록되지 않은 경우, 등록하고 GUID를 반환
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entry    = settings.FindAssetEntry(guid);
            if (entry == null) {
                EditorUtility.DisplayDialog("Error", "Addressable에 등록되지 않은 Sprite입니다.", "OK");
                return null;
            }

            return sliceFlag ? string.Format($"{entry.guid}:{newSprite.name}") : entry.guid;
        }

        private void DrawAsset(string guid) {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entry    = string.IsNullOrEmpty(guid) ? null : settings.FindAssetEntry(guid);
            var obj      = entry != null ? AssetDatabase.LoadAssetAtPath(entry.AssetPath, _type) : null;
            if (obj == null) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                obj = AssetDatabase.LoadAssetAtPath(path, _type);
            }

            if (obj == null) {
                var assetPath = GetAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath) == false) {
                    obj = AssetDatabase.LoadAssetAtPath(assetPath, _type);
                }
            }

            EditorGUI.BeginChangeCheck();
            obj = EditorGUILayout.ObjectField(obj, _type, false);
            if (EditorGUI.EndChangeCheck()) {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(assetPath))
                    return;

                // Addressable에서 에셋 로드
                try {
                    var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                    entry = settings.FindAssetEntry(assetGuid);
                    if (entry?.address != null) {
                        ValueEntry.SmartValue = entry.guid;
                    }
                    else {
                        Debug.LogError("해당 Key에 대해 Addressable에 등록된 에셋이 존재하지 않습니다: " + assetPath);
                    }
                }
                catch (Exception e) {
                    Debug.LogError(e);
                }
            }
        }
    }
}