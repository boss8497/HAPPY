using UnityEngine;

namespace Script.GameInfo.Attribute {
    public class LocalizePathAttribute : PropertyAttribute {
        public string Path       { get; }
        public bool   IsTextArea { get; }

        public LocalizePathAttribute(string path, bool isTextArea = false) {
            this.Path       = path;
            this.IsTextArea = isTextArea;
        }
    }
}