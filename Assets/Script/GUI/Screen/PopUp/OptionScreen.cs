using Cysharp.Threading.Tasks;
using Script.GUI.ScreenData.Interface;
using Script.GUI.ViewModel;

namespace Script.GUI.Screen {
    public class OptionScreen : Screen {
        public AudioOptionViewModel audioOptionViewModel;
        
        
        
        public override UniTask OpenInternal(IScreenOption screenOption) {
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}