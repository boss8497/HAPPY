using System.Threading;
using Cysharp.Threading.Tasks;

namespace Script.Addressable {
    public interface IAddressable {
        bool IsInitialized { get; }

        UniTask LoadAppLabelsAsync(CancellationToken ct = default);

        UniTask<bool> HasInternetConnectionAsync(
            int               timeout    = 3,
            CancellationToken ct = default
        );
    }
}