using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GUI.ScreenData.Interface;

namespace Script.GUI.Screen {
    public class Loading : Screen {
        public override UniTask OpenInternal(IScreenOption screenOption, CancellationToken ct = default) {
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            return UniTask.CompletedTask;
        }
    }
}