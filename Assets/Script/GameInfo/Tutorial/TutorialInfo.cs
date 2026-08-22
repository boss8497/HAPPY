using System;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using UnityEngine;

namespace Script.GameInfo.Info {
    [AutoEditorTable(true)]
    [System.Serializable]
    public class TutorialInfo : InfoBase {
        [SerializeReference]
        public GuideBase[] sets = Array.Empty<GuideBase>();

        public bool systemControl;
        public bool allCloseScreen;
    }
}