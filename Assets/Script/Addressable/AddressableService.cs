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
    public class AddressableService : IAddressableService, IInitializable, IDisposable {
        private readonly string _defaultTestUrl = "https://connectivitycheck.gstatic.com/generate_204";
        private readonly string _appLabels      = $"AppLifetimeScope";

        // 캐시 점검 주기 및, RefCount가 0이 된 후 실제 Release까지 기다리는 유예시간.
        // GameSettingData 로드 전까지 쓰이는 기본값이며, ConfigureCache() 호출 시 갱신된다.
        private float _cacheCheckIntervalSeconds = 600f;
        private float _cacheReleaseGraceSeconds  = 300f;

        public bool IsInitialized { get; private set; }


        private CancellationTokenSource cts;

        private readonly Dictionary<string, CacheEntry> _cache = new();
        private          CancellationTokenSource        _cacheMonitorCts;

        private class CacheEntry {
            public UniTask<AsyncOperationHandle> LoadingTask;
            public AsyncOperationHandle          Handle;
            public int                           RefCount;
            public float                         ReleasePendingSince;
        }

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

            _cacheMonitorCts = new CancellationTokenSource();
            MonitorCacheAsync(_cacheMonitorCts.Token).Forget();
        }

        /// <summary>
        /// AppLabels Group은 Local 이기 때문에 따로 Internet Check가 필요 없음
        /// </summary>
        /// <param name="ct"></param>
        /// <exception cref="Exception"></exception>
        public async UniTask LoadAppLabelsAsync(CancellationToken ct = default) {
            await DownloadDependenciesAsync(_appLabels, null, ct);
        }


        public async UniTask<bool> UpdateCatalogsAsync(
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
                throw new Exception("Addressable UpdateCatalogs failed.");

            return true;
        }

        public async UniTask<long> GetDownloadSizeAsync(
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
                throw new Exception($"Addressable GetDownloadSize failed. key: {key}");
            }

            var size = handle.Result;
            Addressables.Release(handle);
            return size;
        }

        public async UniTask DownloadDependenciesAsync(
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
                throw new Exception($"Addressable DownloadDependencies failed. key: {key}");
        }

        public async UniTask<bool> HasInternetConnectionAsync(
            int               timeout = 3,
            CancellationToken ct      = default
        ) {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return false;

            using var request = UnityWebRequest.Get(_defaultTestUrl);
            request.timeout = timeout;

            var operation = request.SendWebRequest();

            while (!operation.isDone) {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            return request.result == UnityWebRequest.Result.Success
                && request.responseCode == 204;
        }

        public void ConfigureCache(float checkIntervalSeconds, float releaseGraceSeconds) {
            _cacheCheckIntervalSeconds = checkIntervalSeconds;
            _cacheReleaseGraceSeconds  = releaseGraceSeconds;
        }

        public async UniTask<AddressableCacheHandle<T>> LoadAsync<T>(
            string            key,
            CancellationToken ct = default
        ) where T : UnityEngine.Object {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentException("key is null or empty.", nameof(key));
            }

            if (!_cache.TryGetValue(key, out var entry)) {
                entry             = new CacheEntry();
                _cache[key]       = entry;
                // 로딩 Task를 공유해서 동시 요청 시 중복 로드를 막는다.
                // 단, 취소는 최초 요청자의 CancellationToken 기준으로만 동작한다.
                entry.LoadingTask = LoadHandleAsync<T>(key, ct).Preserve();
            }

            entry.RefCount++;

            var handle = await entry.LoadingTask;
            return new((T)handle.Result, () => ReleaseUsage(key));
        }

        private async UniTask<AsyncOperationHandle> LoadHandleAsync<T>(
            string            key,
            CancellationToken ct
        ) where T : UnityEngine.Object {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);

            try {
                while (!handle.IsDone) {
                    ct.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            catch {
                _cache.Remove(key);
                if (handle.IsValid()) Addressables.Release(handle);
                throw;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                _cache.Remove(key);
                Addressables.Release(handle);
                throw new Exception($"Addressable LoadAsset failed. key: {key}");
            }

            _cache[key].Handle = handle;
            return handle;
        }

        private void ReleaseUsage(string key) {
            if (!_cache.TryGetValue(key, out var entry)) return;

            entry.RefCount--;
            if (entry.RefCount <= 0) {
                entry.RefCount            = 0;
                entry.ReleasePendingSince = Time.unscaledTime;
            }
        }

        private async UniTaskVoid MonitorCacheAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                await UniTask.Delay(TimeSpan.FromSeconds(_cacheCheckIntervalSeconds), cancellationToken: ct);
                ReleaseUnusedCache();
            }
        }

        private void ReleaseUnusedCache() {
            var now = Time.unscaledTime;

            List<string> expired = null;
            foreach (var pair in _cache) {
                var entry = pair.Value;
                if (entry.RefCount > 0) continue;
                if (!entry.Handle.IsValid()) continue; // 아직 로딩 중이면 다음 점검으로 미룬다.
                if (now - entry.ReleasePendingSince < _cacheReleaseGraceSeconds) continue;

                Addressables.Release(entry.Handle);
                (expired ??= new List<string>()).Add(pair.Key);
            }

            if (expired == null) return;
            foreach (var key in expired) {
                _cache.Remove(key);
            }
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

            if (_cacheMonitorCts is { IsCancellationRequested: false }) {
                _cacheMonitorCts.Cancel();
            }

            _cacheMonitorCts?.Dispose();

            foreach (var entry in _cache.Values) {
                if (entry.Handle.IsValid()) {
                    Addressables.Release(entry.Handle);
                }
            }

            _cache.Clear();
        }
    }
}