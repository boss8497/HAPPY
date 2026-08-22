using System;
using Script.GameInfo;
using UnityEngine;

namespace Script.Tutorial {
    public enum FocusType {
        None,
        Button,
        Image,
        Toggle,
    }

    [System.Serializable]
    public class TutorialFocusData {
        public SerializeGuid guid = SerializeGuid.NewGuid();
        public string        id;
        public FocusType     type;
        
        public RectTransform rtf;
        
        public Component     target;
        public bool          useGard = true;
        public string        targetType;
        public string        characterGuid;
        public string        itemGuid;
        public Vector2       sizeOffset;
        public Vector2       positionOffset;

        #region Property

        public Vector3       Position => rtf.position + (Vector3)positionOffset;
        public Vector2       Size     => rtf.rect.size + sizeOffset;
        public SerializeGuid Guid     => guid.Value;

        #endregion
    }
}