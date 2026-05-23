using System;
using System.Collections.Generic;
using System.Linq;
using Script.GameInfo.Enum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Script.Editor {
    public sealed class ErrorMessageEditorWindow : OdinEditorWindow {
        private const string TableName             = nameof(ErrorMessage);
        private const string DefaultAssetDirectory = "Assets/Localization/StringTables";

        [MenuItem("Tools/ErrorMessage")]
        private static void Open() {
            var window = GetWindow<ErrorMessageEditorWindow>();
            window.titleContent = new GUIContent("ErrorMessage");
            window.Show();
        }

        [Title("ErrorMessage Localization")]
        [ShowInInspector, ReadOnly]
        private string TargetTableName => TableName;

        [SerializeField]
        [FolderPath]
        [LabelText("Table Asset Directory")]
        private string _assetDirectory = DefaultAssetDirectory;

        [SerializeField]
        [TableList(AlwaysExpanded = true)]
        [Searchable]
        [LabelText("ErrorMessage Entries")]
        private List<ErrorMessageRow> _rows = new();

        protected override void OnEnable() {
            base.OnEnable();
            LoadOrCreate();
        }

        [Button("Load / Create Empty Entries", ButtonSizes.Large)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void LoadOrCreate() {
            var locales = LocalizationEditorSettings.GetLocales();

            if (locales == null || locales.Count <= 0) {
                _rows.Clear();
                Debug.LogWarning(
                    "등록된 Locale이 없습니다. Project Settings > Localization에서 Locale을 먼저 생성해주세요."
                );
                return;
            }

            var collection = GetOrCreateCollection();
            if (collection == null)
                return;

            EnsureLocaleTables(collection, locales);
            EnsureEnumEntries(collection, locales);

            _rows = BuildRows(collection, locales);
            Repaint();

            Debug.Log($"'{TableName}' Localization Table 로드/생성이 완료되었습니다.");
        }

        [Button("Save", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void Save() {
            var locales = LocalizationEditorSettings.GetLocales();

            if (locales == null || locales.Count <= 0) {
                Debug.LogWarning("저장할 Locale이 없습니다.");
                return;
            }

            var collection = GetOrCreateCollection();
            if (collection == null)
                return;

            EnsureLocaleTables(collection, locales);
            EnsureEnumEntries(collection, locales);

            foreach (var row in _rows) {
                if (string.IsNullOrWhiteSpace(row.EnumName))
                    continue;

                if (string.IsNullOrWhiteSpace(row.LocaleCode))
                    continue;

                var localeIdentifier = new LocaleIdentifier(row.LocaleCode);
                var table            = collection.GetTable(localeIdentifier) as StringTable;

                if (table == null) {
                    Debug.LogWarning($"Locale '{row.LocaleCode}'에 해당하는 StringTable을 찾을 수 없습니다.");
                    continue;
                }

                var entry = table.GetEntry(row.EnumName);

                if (entry == null) {
                    entry = table.AddEntry(row.EnumName, string.Empty);
                }

                entry.Value = row.Value ?? string.Empty;

                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);
            }

            EditorUtility.SetDirty(collection.SharedData);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"'{TableName}' Localization 저장 완료");
        }

        [Button("Refresh", ButtonSizes.Medium)]
        private void Refresh() {
            LoadOrCreate();
        }

        private StringTableCollection GetOrCreateCollection() {
            EnsureAssetFolder(_assetDirectory);

            var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);

            if (collection != null)
                return collection;

            collection = LocalizationEditorSettings.CreateStringTableCollection(
                TableName,
                _assetDirectory
            );

            EditorUtility.SetDirty(collection.SharedData);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return collection;
        }

        private static void EnsureLocaleTables(
            StringTableCollection collection,
            IReadOnlyList<Locale> locales
        ) {
            foreach (var locale in locales) {
                var table = collection.GetTable(locale.Identifier) as StringTable;

                if (table != null)
                    continue;

                table = collection.AddNewTable(locale.Identifier) as StringTable;

                if (table == null) {
                    Debug.LogWarning($"Locale '{locale.LocaleName}' StringTable 생성 실패");
                    continue;
                }

                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);
            }
        }

        private static void EnsureEnumEntries(
            StringTableCollection collection,
            IReadOnlyList<Locale> locales
        ) {
            var keys = Enum.GetValues(typeof(ErrorMessage))
                           .Cast<ErrorMessage>()
                           .Select(errorMessage => errorMessage.ToString())
                           .ToArray();

            foreach (var locale in locales) {
                var table = collection.GetTable(locale.Identifier) as StringTable;

                if (table == null)
                    continue;

                foreach (var key in keys) {
                    var entry = table.GetEntry(key);

                    if (entry != null)
                        continue;

                    table.AddEntry(key, string.Empty);
                }

                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);
            }
        }

        private static List<ErrorMessageRow> BuildRows(
            StringTableCollection collection,
            IReadOnlyList<Locale> locales
        ) {
            var rows = new List<ErrorMessageRow>();

            var enumValues = Enum.GetValues(typeof(ErrorMessage))
                                 .Cast<ErrorMessage>();

            foreach (var enumValue in enumValues) {
                var key = enumValue.ToString();

                foreach (var locale in locales) {
                    var table = collection.GetTable(locale.Identifier) as StringTable;
                    var entry = table?.GetEntry(key);

                    rows.Add(new ErrorMessageRow {
                                 EnumName   = key,
                                 LocaleName = locale.LocaleName,
                                 LocaleCode = locale.Identifier.Code,
                                 Value      = entry?.Value ?? string.Empty
                             });
                }
            }

            return rows;
        }

        private static void EnsureAssetFolder(string assetDirectory) {
            if (string.IsNullOrWhiteSpace(assetDirectory)) {
                throw new ArgumentException("Asset directory is empty.");
            }

            if (!assetDirectory.StartsWith("Assets", StringComparison.Ordinal)) {
                throw new ArgumentException("Asset directory must be inside Assets folder.");
            }

            if (AssetDatabase.IsValidFolder(assetDirectory))
                return;

            var parts   = assetDirectory.Split('/');
            var current = parts[0];

            for (int i = 1; i < parts.Length; i++) {
                var next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next)) {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        [Serializable]
        private sealed class ErrorMessageRow {
            [ReadOnly]
            [TableColumnWidth(180)]
            [LabelText("Enum Name")]
            public string EnumName;

            [ReadOnly]
            [TableColumnWidth(160)]
            [LabelText("Localize Language")]
            public string LocaleName;

            [ReadOnly]
            [TableColumnWidth(90)]
            [LabelText("Code")]
            public string LocaleCode;

            [MultiLineProperty(3)]
            [LabelText("Value")]
            public string Value;
        }
    }
}