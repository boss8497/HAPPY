using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

namespace Script.Addressable {
    public class Addressable : IAddressable, IInitializable, IDisposable {
        private const string DefaultTestUrl = "https://connectivitycheck.gstatic.com/generate_204";

        public bool IsInitialized { get; private set; }


        private CancellationTokenSource cts;

        public void Initialize() {
            cts = new();
            InitializeAsync(cts.Token).Forget();
        }

        private async UniTask InitializeAsync(CancellationToken ct) {
            var handle = Addressables.InitializeAsync();

            while (!handle.IsDone) {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                throw new Exception("Addressable Initialize failed.");
            }

            StopAsync();
            IsInitialized = true;
        }


        public static async UniTask<bool> UpdateCatalogsAsync(
            bool              autoCleanBundleCache = true,
            CancellationToken cancellationToken    = default
        ) {
            var checkHandle = Addressables.CheckForCatalogUpdates(autoReleaseHandle: false);

            while (!checkHandle.IsDone) {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (checkHandle.Status != AsyncOperationStatus.Succeeded) {
                Addressables.Release(checkHandle);
                throw new Exception("Addressable CheckForCatalogUpdates failed.");
            }

            var catalogs  = checkHandle.Result;
            var hasUpdate = catalogs != null && catalogs.Count > 0;

            if (!hasUpdate) {
                Addressables.Release(checkHandle);
                return false;
            }

            AsyncOperationHandle<List<IResourceLocator>> updateHandle;

            if (autoCleanBundleCache) {
                updateHandle = Addressables.UpdateCatalogs(
                    autoCleanBundleCache: true,
                    catalogs: catalogs,
                    autoReleaseHandle: false
                );
            }
            else {
                updateHandle = Addressables.UpdateCatalogs(
                    catalogs,
                    autoReleaseHandle: false
                );
            }

            while (!updateHandle.IsDone) {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            var success = updateHandle.Status == AsyncOperationStatus.Succeeded;

            Addressables.Release(updateHandle);
            Addressables.Release(checkHandle);

            if (!success)
                throw new Exception("Addressables UpdateCatalogs failed.");

            return true;
        }

        public static async UniTask<long> GetDownloadSizeAsync(
            object            key,
            CancellationToken cancellationToken = default
        ) {
            var handle = Addressables.GetDownloadSizeAsync(key);

            while (!handle.IsDone) {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                Addressables.Release(handle);
                throw new Exception($"Addressables GetDownloadSize failed. key: {key}");
            }

            var size = handle.Result;
            Addressables.Release(handle);
            return size;
        }

        public static async UniTask DownloadDependenciesAsync(
            object            key,
            IProgress<float>  progress          = null,
            CancellationToken cancellationToken = default
        ) {
            var size = await GetDownloadSizeAsync(key, cancellationToken);

            if (size <= 0) {
                progress?.Report(1f);
                return;
            }

            var handle = Addressables.DownloadDependenciesAsync(
                key,
                autoReleaseHandle: false
            );

            while (!handle.IsDone) {
                cancellationToken.ThrowIfCancellationRequested();

                var status = handle.GetDownloadStatus();
                progress?.Report(status.Percent);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            progress?.Report(1f);

            var success = handle.Status == AsyncOperationStatus.Succeeded;
            Addressables.Release(handle);

            if (!success)
                throw new Exception($"Addressables DownloadDependencies failed. key: {key}");
        }

        public static async UniTask<bool> HasInternetConnectionAsync(
            int               timeoutSeconds    = 3,
            CancellationToken cancellationToken = default
        ) {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return false;

            using var request = UnityWebRequest.Get(DefaultTestUrl);
            request.timeout = timeoutSeconds;

            var operation = request.SendWebRequest();

            while (!operation.isDone) {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            return request.result == UnityWebRequest.Result.Success
                && request.responseCode == 204;
        }

        private void StopAsync() {
            if (cts is { IsCancellationRequested: false }) {
                cts.Cancel();
            }

            cts?.Dispose();
            cts = null;
        }

        public void Dispose() {
            if (cts is { IsCancellationRequested: false }) {
                cts.Cancel();
            }

            cts?.Dispose();
        }
    }
}