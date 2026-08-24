using System.Threading;
using Cysharp.Threading.Tasks;

namespace Script.Tutorial.Interface {
    public interface ITutorialScreen {
        public UniTask StopAsync(bool                  hide = true, CancellationToken ct = default);
    }
}