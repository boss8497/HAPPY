using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Script.GameInfo.Info;
using SW.GUI.Base;

namespace Script.Tutorial.Interface {
    public interface ITutorialFocus : ITutorialScreen {
        public SW_GUI_BUTTON_BASE FocusButton { get; }

        public UniTask SetFocusAsync(TutorialFocusData focusData,   FocusGuide        focusGuide, CancellationToken ct = default);
        public void    SetGardAlpha(bool               isOn);
        public void    SetButton(bool                  enable);
    }
}