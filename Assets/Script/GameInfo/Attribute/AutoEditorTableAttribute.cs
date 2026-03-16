using System;
using UnityEngine;

namespace Script.GameInfo.Attribute {
    
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AutoEditorTableAttribute : PropertyAttribute {
        public bool UseSerializeReference { get; }

        public AutoEditorTableAttribute(bool useSerializeReference = false) {
            UseSerializeReference = useSerializeReference;
        }
    }
}