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
    }
}