using Cysharp.Threading.Tasks;
using Script.GUI.ScreenData.Interface;

namespace Script.GUI.Screen {
    public class OptionScreen : Screen {
        public override UniTask OpenInternal(IScreenOption screenOption) {
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}