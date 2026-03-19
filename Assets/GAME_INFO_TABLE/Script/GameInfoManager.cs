using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Base;
using Script.GameInfo.Info;
using Script.GameInfo.Table.Interface;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace Script.GameInfo.Table {
    public partial class GameInfoManager : IGameInfoManager {
        private readonly Dictionary<string, CacheTable>                      _cacheTables       = new();
        private readonly Dictionary<Type, CacheTable>                        _cacheTablesByType = new();
        
        private readonly Dictionary<string, AsyncOperationHandle<TableBase>> _handles           = new();

        private static IGameInfoManager _instance;
        public static IGameInfoManager Instance {
            get {
                if (_instance == null) {
                    _instance = new GameInfoManager();
                    _instance.Load();
                }

                return _instance;
            }
        }
        private bool _dirty;


        private ConfigurationInfo _config;
        public  ConfigurationInfo Config => _config;

        
        
        public async UniTask LoadAsync() {
            foreach (var key in AddressableKeys) {
                if (_cacheTables.ContainsKey(key))
                    continue;

                var handle = Addressables.LoadAssetAsync<TableBase>(key);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                    throw new Exception($"Load failed: {key}");

                _cacheTables[key] = new CacheTable(key, handle.Result);
                _cacheTablesByType[handle.Result.ElementType] = _cacheTables[key];
                _handles[key]     = handle;
            }


            //GameConfig Load
            var configHandle = Addressables.LoadAssetAsync<GameConfiguration>(nameof(GameConfiguration));
            await configHandle.Task;
            
            if (configHandle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Load failed: {nameof(GameConfiguration)}");

            _config = configHandle.Result.config.Clone();
            Addressables.Release(configHandle);
        }

        public void Load() {
            foreach (var key in AddressableKeys) {
                if (_cacheTables.ContainsKey(key))
                    continue;


                var handle = Addressables.LoadAssetAsync<TableBase>(key);
                handle.WaitForCompletion();

                if (handle.Status != AsyncOperationStatus.Succeeded)
                    throw new Exception($"Load failed: {key}");

                _cacheTables[key]                             = new CacheTable(key, handle.Result);
                _cacheTablesByType[handle.Result.ElementType] = _cacheTables[key];
                _handles[key]                                 = handle;
            }
            
            
            //GameConfig Load
            var configHandle = Addressables.LoadAssetAsync<GameConfiguration>(nameof(GameConfiguration));
            configHandle.WaitForCompletion();
            
            if (configHandle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Load failed: {nameof(GameConfiguration)}");

            _config = configHandle.Result.config.Clone();
            Addressables.Release(configHandle);
        }

        public T Get<T>(int uid) where T : InfoBase {
            var type = typeof(T);
            if (_cacheTablesByType.TryGetValue(type, out var cacheTable)) {
                return cacheTable.Get<T>(uid);
            }
            return null;
        }
        
        public T[] GetCollection<T>() where T : InfoBase {
            var type = typeof(T);
            
            if (_cacheTablesByType.TryGetValue(type, out var cacheTable)) {
                return cacheTable.GetCollection<T>();
            }

            return Array.Empty<T>();
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
                    if (_cacheTables.TryGetValue(key, out var cacheTable)) {
                        _cacheTablesByType.Remove(cacheTable.Table.ElementType);
                        cacheTable.Release();
                    }
                    _cacheTables.Remove(key);
                }
            }
            
            _cacheTables.Clear();
            _cacheTablesByType.Clear();
        }
    }
}