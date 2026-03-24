using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Script.GamePlay.Pool {
    [System.Serializable]
    public class ObjectPoolSystem : IObjectPoolSystem, IDisposable {
        private readonly IPooling          _manager;
        private readonly string            _key;
        private readonly Stack<GameObject> _stack = new();

        private AsyncOperationHandle<GameObject> _handle;
        private GameObject                       _instance;


        public string Key => _key;


        public ObjectPoolSystem(IPooling manager, string key, int count = 5) {
            _manager = manager;
            _key     = key;
            Initialize();
            CreateInstance(count);
        }

        public void Initialize() {
            _handle = Addressables.LoadAssetAsync<GameObject>(_key);
            _handle.WaitForCompletion();
            
            _instance = UnityEngine.Object.Instantiate(_handle.Result);
            _instance.gameObject.SetActive(false);

            if (_instance.TryGetComponent<PoolMember>(out var member) == false) {
                member = _instance.AddComponent<PoolMember>();
            }

            member.Set(this);

            Addressables.Release(_handle);
        }

        private void CreateInstance(int count = 1) {
            for (int i = 0; i < count; i++) {
                _stack.Push(UnityEngine.Object.Instantiate(_instance));
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
            Addressables.Release(_handle);
            if (_instance != null) {
                UnityEngine.Object.Destroy(_instance);
            }

            foreach (var item in _stack) {
                UnityEngine.Object.Destroy(item);
            }
        }
    }
}