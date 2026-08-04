using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Script.Utility.Runtime {
    public class AddressableHandle<T> : IDisposable where T : class {
        private AsyncOperationHandle<T> _handle;
        private bool                    _hasHandle;
        private string                  _path;


        public T Load(string path) {
            if (string.IsNullOrEmpty(path)) {
                Release();
                return null;
            }

            if (_path.Equals(path)) return _handle.Result;

            _path      = path;
            _handle    = Addressables.LoadAssetAsync<T>(path);
            _hasHandle = true;
            return _handle.WaitForCompletion();
        }

        public async UniTask<T> LoadAsync(string path) {
            if (string.IsNullOrEmpty(path)) {
                Release();
                return null;
            }
            if (_path.Equals(path)) return _handle.Result;

            _path      = path;
            _handle    = Addressables.LoadAssetAsync<T>(path);
            _hasHandle = true;
            return await _handle.Task;
        }

        private void Release() {
            if (_hasHandle) {
                if (_handle.IsValid()) {
                    Addressables.Release(_handle);
                }

                _path      = string.Empty;
                _hasHandle = false;
            }
        }

        public void Dispose() => Release();
    }
}