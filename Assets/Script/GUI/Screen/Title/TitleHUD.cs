using Cysharp.Threading.Tasks;

namespace Script.GUI.Screen {
    public class TitleHUD : Screen {
        public override UniTask OpenInternal() {
            return UniTask.CompletedTask;
        }
        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}