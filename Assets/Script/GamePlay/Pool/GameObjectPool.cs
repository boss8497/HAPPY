using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Script.Utility.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer.Unity;

namespace Script.GamePlay.Pool {
    [System.Serializable]
    public class GameObjectPool : IGameObjectPool, IDisposable {
        private readonly IStagePooling     _manager;
        private readonly string            _key;
        private readonly Stack<GameObject> _stack;

        private GameObject _instance;
        private bool       _isDisposed;

        public string Key => _key;


        public GameObjectPool(IStagePooling manager, string key, int count = 1) {
            _stack   = ListPool.GetCollection<Stack<GameObject>>();
            _manager = manager;
            _key     = key;
            Initialize();
            CreateInstance(count);
        }

        public void Initialize() {
            var handle = Addressables.LoadAssetAsync<GameObject>(_key);
            handle.WaitForCompletion();

            _instance = _manager.Resolver.Instantiate(handle.Result, _manager.Root);
            _instance.gameObject.SetActive(false);
            
            Addressables.Release(handle);
        }

        private void CreateInstance(int count = 1) {
            for (int i = 0; i < count; i++) {
                // 생성될 때 이미 Active는 False인 상태
                var obj = _manager.Resolver.Instantiate(_instance, _manager.Root);
                if (obj.TryGetComponent<IPoolMember>(out var member) == false) {
                    member = _instance.AddComponent<PoolMember>();
                }
                member.Set(this);
                
                _stack.Push(obj);
            }
        }

        public GameObject Pop() {
            if (_stack.Count <= 0) {
                CreateInstance();
            }

            return _stack.Pop();
        }

        public void Push(GameObject obj) {
            _stack.Push(obj);
        }

        public void Dispose() {
            if (_isDisposed) return;

            if (_instance != null) {
                UnityEngine.Object.Destroy(_instance);
            }

            foreach (var item in _stack) {
                UnityEngine.Object.Destroy(item);
            }
            
            _stack.Clear();
            ListPool.ReturnCollection(_stack);

            _isDisposed = true;
        }
    }
}