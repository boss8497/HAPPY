using System.Threading;
using Cysharp.Threading.Tasks;

namespace Script.GamePlay.Scene {
    public interface ISceneLoader {
        UniTask LoadScene(string                 scenePath , CancellationToken ct = default);
    }
}