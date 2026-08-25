namespace Script.GameInfo.Info {
    
    /// <summary>
    /// 규칙: 순차 증가해야 됨
    /// 위험: 중간에 튜토리얼 삽입 시 데이터 마이그레이션 필요.
    /// 위치 -> TutorialInfo.cs 
    /// </summary>
    public enum TutorialProgress {
        None = 0,
        FirstLobbyConnection = 1, // 첫 로비화면 들어왔을 때
    }
}