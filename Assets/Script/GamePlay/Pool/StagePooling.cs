using System;
using System.Collections.Generic;
using Script.Utility.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Script.GamePlay.Pool {
    public class StagePooling : IInitializable, IDisposable, IStagePooling {
        private readonly Dictionary<string, GameObjectPool> _objectPools = new(StringComparer.Ordinal);

        public Transform       Root     { get; private set; }
        public IObjectResolver Resolver { get; private set; }


        public StagePooling(IObjectResolver resolver) {
            Resolver = resolver;
        }

        public void Initialize() {
            var obj = new GameObject("StagePoolingRoot");
            Root = obj.transform;
            Root.position = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
        }

        public bool Exists(string key) {
            return _objectPools.ContainsKey(key);
        }


        public GameObject Pop(string key, Transform parent = null, bool active = true) {
            if (_objectPools.TryGetValue(key, out var pool) == false) {
                pool = CreatePool(key);
            }

            var obj = pool.Pop();
            obj.SetSafeActive(active);
            obj.transform.SetParent(parent);
            return obj;
        }

        private GameObjectPool CreatePool(string key) {
            var pool = new GameObjectPool(this, key);
            _objectPools.Add(key, pool);
            return pool;
        }

        public bool Push(GameObject obj) {
            obj.SetSafeActive(false);
            
            if (obj.TryGetComponent<PoolMember>(out var member) == false) {
                Object.Destroy(obj);
                return false;
            }

            if (_objectPools.TryGetValue(member.Key, out var pool)) {
                pool.Push(obj);
                return true;
            }

            Object.Destroy(obj);
            return false;
        }

        private void Release() {
            foreach (var pool in _objectPools) {
                pool.Value.Dispose();
            }

            _objectPools.Clear();
            
            if (Root) {
                Object.Destroy(Root);
            }
        }

        public void Dispose() {
            Release();
        }
    }
}