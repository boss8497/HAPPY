using Script.GUI.Enum;

namespace Script.GUI {
    public partial class ScreenManager {
        private ScreenManagerState _state = ScreenManagerState.None;
        public  ScreenManagerState State  => _state;
        
        public bool Initialized   => (_state & ScreenManagerState.Initialized) != 0;
        
        
        public void AddState(ScreenManagerState screenManagerState) {
            if (_state.HasFlag(screenManagerState)) return;
            _state |= screenManagerState;
        }

        public void RemoveState(ScreenManagerState screenManagerState) {
            if (_state.HasFlag(screenManagerState) == false) return;
            _state &= ~screenManagerState;
        }
    }
}