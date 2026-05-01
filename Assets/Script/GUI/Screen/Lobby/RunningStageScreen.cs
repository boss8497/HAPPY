using Cysharp.Threading.Tasks;

namespace Script.GUI.Screen {
    public class RunningStageScreen : Screen{
        public override UniTask OpenInternal() {
            return UniTask.CompletedTask;
        }
        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}