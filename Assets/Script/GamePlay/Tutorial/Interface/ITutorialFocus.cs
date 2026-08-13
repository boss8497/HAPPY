using Cysharp.Threading.Tasks;
using R3;
using Script.GameInfo.Info;
using SW.GUI.Base;

namespace Script.Tutorial.Interface {
    public interface ITutorialFocus {
        ReadOnlyReactiveProperty<bool> IsScreenShow { get; }
        public SW_GUI_BUTTON_BASE      FocusButton  { get; }

        public UniTask SetFocusAsync(TutorialFocusData     focusData, FocusGuide focusGuide);
        public UniTask SetFocusAnimation(TutorialFocusData focusData, FocusGuide focusGuide);
        public UniTask StopAsync(bool                      hide = true);
        public UniTask ScreenHide();
        public void    SetGardAlpha(bool isOn);
        public void    SetButton(bool    enable);
    }
}