using System;
using Script.GameInfo.Attribute;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Script.GameInfo.Base {
    [System.Serializable]
    public abstract class InfoBase{
        [field: SerializeField]
        public int    UID { get; set; }
        
        [field: SerializeField]
        public string ID  { get; set; }

        [field: SerializeField]
        [LocalizePath("@\"Character/Name/\" + UID", false)]
        public string Name { get; set; }

        [field: SerializeField]
        public IComponent[] Components { get; set; } = Array.Empty<IComponent>();

        public virtual bool ValidData() {
            if (ValidUid(UID)) return false;
            return true;
        }

        public static bool ValidUid(int uid) {
            if (uid <= 0) return false;
            return true;
        }
    }
}