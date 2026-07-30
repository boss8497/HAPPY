using Cysharp.Threading.Tasks;
using Script.GUI.ScreenData.Interface;
using Script.GUI.ViewModel;
using SW.GUI;

namespace Script.GUI.Screen {
    public class OptionScreen : Screen {
        public AudioOptionViewModel audioOptionViewModel;


        public SW_GUI_BUTTON changedButton; 
        
        public override UniTask OpenInternal(IScreenOption screenOption) {
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}