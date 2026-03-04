using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace Script.Utility {
    public static class LocalizeUtility {
        private const           string           LocalizationDirectory = "Assets/Localization";
        private static readonly LocaleIdentifier KoreanLocale          = new("ko");
        private static readonly Regex            TermRegex             = new(@"^(?<table>[^/]+)/(?<entry>.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private static StringTableCollection GetStringTableCollection(string collectionName) {
            return LocalizationEditorSettings.GetStringTableCollection(collectionName);
        }

        private static void DecomposeLocalizeKey(string term, out string tableName, out string entryName) {
            var m = TermRegex.Match(term);
            if (!m.Success)
                throw new ArgumentException("term is not valid. term must be {CollectionName}/{EntryName} format. - " + term, nameof(term));

            tableName = m.Groups["table"].Value;
            entryName = m.Groups["entry"].Value;
        }

        public static string GetLocalizeText(string term) {
            DecomposeLocalizeKey(term, out var tableName, out var entryName);

            var collection = GetStringTableCollection(tableName);
            if (collection == null)
                return null;

            var table = collection.GetTable(KoreanLocale) as StringTable;
            if (table == null)
                return null;

            var entry = table.GetEntry(entryName);
            return entry?.Value;
        }

        public static bool ContainsLocalizeKey(string term) {
            return GetLocalizeText(term) != null;
        }

        public static void RemoveLocalizeText(string term) {
            DecomposeLocalizeKey(term, out var tableName, out var entryName);

            var collection = GetStringTableCollection(tableName);
            if (collection == null)
                return;

            collection.RemoveEntry(entryName);
            EditorUtility.SetDirty(collection);
        }

        public static string SetLocalizeText(string term, string text) {
            DecomposeLocalizeKey(term, out var tableName, out var entryName);

            var collection = GetStringTableCollection(tableName);
            if (collection == null)
                return null;

            var table = collection.GetTable(KoreanLocale) as StringTable;
            if (table == null)
                return null;

            var sharedTableEntry = table.SharedData.GetEntry(entryName);
            table.AddEntry(entryName, text);

            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
            return text;
        }

        public static bool HasLocalizeText(string term) {
            DecomposeLocalizeKey(term, out var tableName, out var entryName);

            var collection = GetStringTableCollection(tableName);
            if (collection == null)
                return false;

            var table = collection.GetTable(KoreanLocale) as StringTable;
            if (table == null)
                return false;

            var entry = table.GetEntry(entryName);
            return entry != null;
        }

        public static void CreateLocalizeText(string term, string text) {
            if (string.IsNullOrWhiteSpace(text)) {
                RemoveLocalizeText(term);
                return;
            }

            DecomposeLocalizeKey(term, out var tableName, out var entryName);

            var collection = GetStringTableCollection(tableName);
            if (collection == null) {
                collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, LocalizationDirectory);
            }

            var table = collection.GetTable(KoreanLocale) as StringTable;
            if (table == null) {
                table = collection.AddNewTable(KoreanLocale) as StringTable;
                EditorUtility.SetDirty(collection);
            }

            var entry            = table.AddEntry(entryName, text);
            var sharedTableEntry = table.SharedData.GetEntry(entryName);

            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }

        public static void SetLocalizeDescription(string term, string description) {
            DecomposeLocalizeKey(term, out var tableName, out var entryName);

            var collection = GetStringTableCollection(tableName);
            if (collection == null)
                return;

            var sharedTableEntry = collection.SharedData
                                             .GetEntry(entryName);

            if (sharedTableEntry == null)
                return;

            var meta = sharedTableEntry.Metadata;
            if (meta.HasMetadata<Comment>()) {
                var comment = meta.GetMetadata<Comment>();
                if (string.IsNullOrEmpty(description)) {
                    meta.RemoveMetadata(comment);
                }
                else {
                    comment.CommentText = description;
                }
            }
            else if (!string.IsNullOrEmpty(description)) {
                meta.AddMetadata(
                    new Comment() { CommentText = description });
            }

            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
        }

        public static string LocalizeTextField(GUIContent name, string term, string description = "") {
            var text = GetLocalizeText(term);

            EditorGUI.BeginChangeCheck();
            var newText = EditorGUILayout.TextField(name, text);

            if (EditorGUI.EndChangeCheck() && newText != text) {
                if (string.IsNullOrEmpty(newText)) {
                    RemoveLocalizeText(term);
                }
                else {
                    CreateLocalizeText(term, newText);
                    SetLocalizeDescription(term, description);
                }
            }

            return newText;
        }

        public static string LocalizeTextField(Rect rect, GUIContent name, string term, string description = "") {
            var text = GetLocalizeText(term);

            EditorGUI.BeginChangeCheck();
            text = EditorGUI.TextField(rect, name, text);

            if (EditorGUI.EndChangeCheck()) {
                if (string.IsNullOrEmpty(text)) {
                    RemoveLocalizeText(term);
                }
                else {
                    CreateLocalizeText(term, text);
                    SetLocalizeDescription(term, description);
                }
            }

            return text;
        }

        public static string LocalizeTextArea(string term, string description = "") {
            var text = GetLocalizeText(term);

            EditorGUI.BeginChangeCheck();
            var returnCount = text?.Count(c => c == '\n') ?? 0;
            text = EditorGUILayout.TextArea(
                text,
                GUILayout.Height(EditorGUIUtility.singleLineHeight * (returnCount + 1)),
                GUILayout.ExpandHeight(false)
            );

            if (EditorGUI.EndChangeCheck()) {
                if (string.IsNullOrEmpty(text)) {
                    RemoveLocalizeText(term);
                }
                else {
                    CreateLocalizeText(term, text);
                    SetLocalizeDescription(term, description);
                }
            }

            return text;
        }

        public static string LocalizeTextArea(Rect rect, string term) {
            var text = GetLocalizeText(term);

            EditorGUI.BeginChangeCheck();
            text = EditorGUI.TextArea(rect, text);

            if (EditorGUI.EndChangeCheck()) {
                if (string.IsNullOrEmpty(text)) {
                    RemoveLocalizeText(term);
                }
                else {
                    CreateLocalizeText(term, text);
                }
            }

            return text;
        }


        public static (string key, SharedTableData.SharedTableEntry entry)[] PrintAllKeys() {
            return LocalizationEditorSettings
                   .GetStringTableCollections()
                   .SelectMany(s => s.SharedData.Entries.Select(entry => ($"{s.TableCollectionName}/{entry.Key}", entry)))
                   .ToArray();
        }

        public static string[] GetAllKeys() {
            return LocalizationEditorSettings
                   .GetStringTableCollections()
                   .SelectMany(s => s.SharedData.Entries.Select(entry => $"{s.TableCollectionName}/{entry.Key}"))
                   .ToArray();
        }
    }
}