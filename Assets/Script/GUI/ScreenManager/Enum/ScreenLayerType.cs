namespace Script.GUI.Screen.Enum {
    public enum ScreenLayerType {
        HUD = 0, // HUD
        None,    // ScreenLayer를 셋팅 안하면 기본적으로 쌓이는 Layer
        Popup,
        Overlay,
        StageTransition, // 스테이지 시작/재시작 시 화면을 얼려 덮는 전환 오버레이 전용 Layer (ScreenManager.StageTransition.cs)
        Loading,
        SafeArea,        // Screen Load 시 터치를 막을 Layer
        Max,
    }
}