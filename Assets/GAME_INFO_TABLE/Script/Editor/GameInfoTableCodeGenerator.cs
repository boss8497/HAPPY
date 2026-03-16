#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using Script.GameInfo.Base;
using UnityEditor;
using UnityEngine;
using Script.GameInfo.Attribute;


namespace Script.GameInfo.Table.Editor {
    public static class GameInfoTableCodeGenerator {
        private const string OutputFolder = "Assets/GAME_INFO_TABLE/Script/Table/Generated";

        [MenuItem("Tools/GameInfo/Generate Missing Tables")]
        public static void GenerateMissingTables() {
            EnsureFolder(OutputFolder);

            var infoTypes = TypeCache.GetTypesDerivedFrom<InfoBase>()
                                     .Where(IsValidInfoType)
                                     .Where(HasAutoEditorTableAttribute)
                                     .OrderBy(t => t.FullName)
                                     .ToArray();

            var tableTypes = TypeCache.GetTypesDerivedFrom<TableBase>()
                                      .Where(IsValidTableType)
                                      .ToArray();

            var existingManagedInfoTypes = tableTypes
                                           .Select(GetManagedInfoType)
                                           .Where(t => t != null)
                                           .ToHashSet();

            int generatedCount = 0;

            foreach (var infoType in infoTypes) {
                if (existingManagedInfoTypes.Contains(infoType))
                    continue;

                var attribute = GetAutoEditorTableAttribute(infoType);
                var code      = GenerateTableCode(infoType, attribute);

                var fileName = $"{GetTableTypeName(infoType)}.generated.cs";
                var path     = Path.Combine(OutputFolder, fileName).Replace("\\", "/");

                File.WriteAllText(path, code, new UTF8Encoding(false));
                generatedCount++;

                Debug.Log($"[GameInfoTableCodeGenerator] Generated: {path}");
            }

            AssetDatabase.Refresh();

            Debug.Log($"[GameInfoTableCodeGenerator] Done. Generated {generatedCount} missing table(s).");
        }

        private static bool IsValidInfoType(Type type) {
            if (type == null)
                return false;

            if (!typeof(InfoBase).IsAssignableFrom(type))
                return false;

            if (type.IsAbstract || type.IsGenericTypeDefinition)
                return false;

            if (type.IsNested)
                return false;

            return true;
        }

        private static bool IsValidTableType(Type type) {
            if (type == null)
                return false;

            if (!typeof(TableBase).IsAssignableFrom(type))
                return false;

            if (type.IsAbstract || type.IsGenericTypeDefinition)
                return false;

            return true;
        }

        private static bool HasAutoEditorTableAttribute(Type type) {
            return GetAutoEditorTableAttribute(type) != null;
        }

        private static AutoEditorTableAttribute GetAutoEditorTableAttribute(Type type) {
            return type.GetCustomAttributes(typeof(AutoEditorTableAttribute), false)
                       .FirstOrDefault() as AutoEditorTableAttribute;
        }

        private static Type GetManagedInfoType(Type tableType) {
            try {
                var instance = ScriptableObject.CreateInstance(tableType) as TableBase;
                if (instance == null)
                    return null;

                var elementType = instance.ElementType;
                UnityEngine.Object.DestroyImmediate(instance);
                return elementType;
            }
            catch (Exception e) {
                Debug.LogWarning($"[GameInfoTableCodeGenerator] Failed to inspect table type: {tableType.FullName}\n{e}");
                return null;
            }
        }

        private static string GenerateTableCode(Type infoType, AutoEditorTableAttribute attribute) {
            var infoTypeName          = GetTypeNameForCode(infoType);
            var infoNamespace         = infoType.Namespace;
            var tableTypeName         = GetTableTypeName(infoType);
            var fieldName             = GetCollectionFieldName(infoType);
            var useSerializeReference = attribute != null && attribute.UseSerializeReference;

            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using Script.GameInfo.Base;");
            sb.AppendLine("using UnityEngine;");

            if (!string.IsNullOrEmpty(infoNamespace) && infoNamespace != "Script.GameInfo.Table")
                sb.AppendLine($"using {infoNamespace};");

            sb.AppendLine();
            sb.AppendLine("namespace Script.GameInfo.Table {");
            sb.AppendLine("    [System.Serializable]");
            sb.AppendLine($"    [CreateAssetMenu(fileName = \"{tableTypeName}\", menuName = \"Data/Table/{tableTypeName}\")]");
            sb.AppendLine($"    public partial class {tableTypeName} : TableBase {{");
            sb.AppendLine("        public override InfoBase[] Infos {");
            sb.AppendLine($"            get => {fieldName}.OfType<InfoBase>().ToArray();");
            sb.AppendLine("            set {");
            sb.AppendLine("                if (value == null) {");
            sb.AppendLine($"                    {fieldName} = Array.Empty<{infoTypeName}>();");
            sb.AppendLine("                    return;");
            sb.AppendLine("                }");
            sb.AppendLine();
            sb.AppendLine($"                var typedInfos = value.OfType<{infoTypeName}>().ToArray();");
            sb.AppendLine("                if (typedInfos.Length != value.Length) {");
            sb.AppendLine($"                    Debug.LogError($\"모든 요소가 {infoTypeName} 타입이 아닙니다.\");");
            sb.AppendLine("                    return;");
            sb.AppendLine("                }");
            sb.AppendLine();
            sb.AppendLine($"                {fieldName} = typedInfos;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override Type ElementType {");
            sb.AppendLine("            get {");
            sb.AppendLine($"                _type ??= typeof({infoTypeName});");
            sb.AppendLine("                return _type;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        [NonSerialized]");
            sb.AppendLine("        private Type _type;");
            sb.AppendLine();

            if (useSerializeReference)
                sb.AppendLine("        [SerializeReference]");
            else
                sb.AppendLine("        [field: SerializeField]");

            sb.AppendLine($"        public {infoTypeName}[] {fieldName} = Array.Empty<{infoTypeName}>();");
            sb.AppendLine();
            sb.AppendLine("        public override T[] GetCollection<T>() {");
            sb.AppendLine($"            if ({fieldName} is T[] collection)");
            sb.AppendLine("                return collection;");
            sb.AppendLine();
            sb.AppendLine("            return Array.Empty<T>();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GetTableTypeName(Type infoType) {
            var name = infoType.Name;

            if (name.EndsWith("Info", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "Info".Length);

            return $"{name}Table";
        }

        private static string GetCollectionFieldName(Type infoType) {
            var name = infoType.Name;

            if (name.EndsWith("Info", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "Info".Length);

            return $"{name}Infos";
        }

        private static string GetTypeNameForCode(Type type) {
            if (!type.IsGenericType)
                return type.Name;

            var genericTypeName = type.Name;
            var tickIndex       = genericTypeName.IndexOf('`');
            if (tickIndex >= 0)
                genericTypeName = genericTypeName.Substring(0, tickIndex);

            var genericArgs = type.GetGenericArguments()
                                  .Select(GetTypeNameForCode);

            return $"{genericTypeName}<{string.Join(", ", genericArgs)}>";
        }

        private static void EnsureFolder(string folderPath) {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parts   = folderPath.Split('/');
            var current = parts[0];

            for (int i = 1; i < parts.Length; i++) {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
    }
}


#endif