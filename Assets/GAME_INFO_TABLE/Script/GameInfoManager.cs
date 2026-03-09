using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Base;
using Script.GameInfo.Table.Interface;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace Script.GameInfo.Table {
    public partial class GameInfoManager : IGameInfoManager {
        private readonly Dictionary<string, TableBase>                       _tables  = new();
        private readonly Dictionary<string, AsyncOperationHandle<TableBase>> _handles = new();

        private IGameInfoManager _instance;
        public IGameInfoManager Instance {
            get {
                if (_instance == null) {
                    _instance = new GameInfoManager();
                    _instance.Load();
                }
                return _instance;
            }
        }
        
        private bool _dirty;

        public async UniTask LoadAsync() {
            foreach (var key in AddressableKeys) {
                if (_tables.TryGetValue(key, out var cached))
                    continue;

                var handle = Addressables.LoadAssetAsync<TableBase>(key);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                    throw new Exception($"Load failed: {key}");

                _tables[key]  = handle.Result;
                _handles[key] = handle;
            }
        }
        
        public void Load() {
            foreach (var key in AddressableKeys) {
                if (_tables.TryGetValue(key, out var cached))
                    continue;

                var handle = Addressables.LoadAssetAsync<TableBase>(key);
                handle.WaitForCompletion();

                if (handle.Status != AsyncOperationStatus.Succeeded)
                    throw new Exception($"Load failed: {key}");

                _tables[key]  = handle.Result;
                _handles[key] = handle;
            }
        }

        public async UniTask Flush() {
            if (_dirty) {
                _dirty = false;
                await LoadAsync();
            }
        }

        public void Dirty() {
            _dirty = true;
        }


        public void Release() {
            foreach (var key in AddressableKeys) {
                if (_handles.TryGetValue(key, out var handle)) {
                    Addressables.Release(handle);
                    _handles.Remove(key);
                    _tables.Remove(key);
                }
            }
        }
    }
}