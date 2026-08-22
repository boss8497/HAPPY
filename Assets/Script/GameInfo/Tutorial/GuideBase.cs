namespace Script.GameInfo.Info{ 
    /// <summary>
    /// TutorialInfo가 Has로 가지고 있는 가이드의 정보
    /// 현재는 Focus만 존재
    /// </summary>
    [System.Serializable]
    public abstract class GuideBase {
        public string id;
        public float  delayTime;
        public float  fadeInTime;
        public float  fadeOutTime;
    }
}