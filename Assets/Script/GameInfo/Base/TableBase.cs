using System;
using UnityEngine;

namespace Script.GameInfo.Base {
    [System.Serializable]
    public abstract class TableBase : ScriptableObject {
        public abstract InfoBase[] Infos { get; }
    }
}