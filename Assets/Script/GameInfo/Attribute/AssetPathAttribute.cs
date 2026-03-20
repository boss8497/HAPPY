
using System;
using UnityEngine;

namespace Script.GameInfo.Attribute {
    public class AssetPathAttribute : PropertyAttribute {
        public Type Type;
        public AssetPathAttribute(Type type) => this.Type = type;
    }
}