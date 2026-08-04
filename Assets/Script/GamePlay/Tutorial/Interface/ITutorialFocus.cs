using Cysharp.Threading.Tasks;
using R3;
using SW.GUI.Base;

namespace Script.Tutorial.Interface {
    public interface ITutorialFocus {
        ReadOnlyReactiveProperty<bool> IsScreenShow { get; }
        public SW_GUI_BUTTON_BASE      FocusButton  { get; }

        //public UniTask SetFocusAsync(FocusData     focusData, FocusGuide focusGuide);
        //public UniTask SetFocusAnimation(FocusData focusData, FocusGuide focusGuide);
        public UniTask StopAsync(bool              hide = true);
        public UniTask ScreenHide();
        public void    SetGardAlpha(bool isOn);
        public void    SetButton(bool    enable);
    }
}