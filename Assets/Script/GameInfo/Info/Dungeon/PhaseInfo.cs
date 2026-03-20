using System;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Script.GameInfo.Info.Dungeon {
    [AutoEditorTable(true)]
    [Serializable]
    public class PhaseInfo : InfoBase {
        [SerializeReference]
        public TriggerBase[] triggers = Array.Empty<TriggerBase>();


        [SerializeReference]
        public ActionBase[] actions = Array.Empty<ActionBase>();
    }
}