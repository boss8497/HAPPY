namespace Script.GUI.Screen.Enum {
    public enum ScreenLayerType {
        HUD = 0, // HUD
        None,    // ScreenLayer를 셋팅 안하면 기본적으로 쌓이는 Layer
        Popup,
        Overlay,
        Loading,
        SafeArea, // Screen Load 시 터치를 막을 Layer
        Max,
    }
}