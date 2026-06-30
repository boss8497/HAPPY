using System;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using UnityEngine;

namespace Script.GameInfo.Info {
    [AutoEditorTable(true)]
    [System.Serializable]
    public class BuffInfo : InfoBase {
        [Status]
        public int[] statusUid = Array.Empty<int>();

        public float time;

        // 버프 발동 후 최대 속도까지 올라가는 시간 (0이면 즉시 적용)
        public float fadeInTime;

        // 버프 종료 전 속도가 줄어드는 시간 (0이면 즉시 제거, time 총 지속 시간에 포함)
        public float fadeOutTime;

        [AssetPath(typeof(Sprite))]
        public string icon;
    }
}