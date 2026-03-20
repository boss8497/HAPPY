using System;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using UnityEngine;

namespace Script.GameInfo.Dungeon {
    [AutoEditorTable(true)]
    [Serializable]
    public class PhaseInfo : InfoBase {
        [SerializeReference]
        public TriggerBase[] triggers = Array.Empty<TriggerBase>();


        [SerializeReference]
        public ActionBase[] actions = Array.Empty<ActionBase>();
    }
}