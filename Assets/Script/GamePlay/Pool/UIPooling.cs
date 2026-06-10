using System;
using System.Collections.Generic;
using Script.LifetimeScope.Locator;
using Script.Utility.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Script.GamePlay.Pool {
    /// <summary>
    /// Screen에서 사용할 Pooling들 ScrollRect의 Element 등 UI Pool
    /// </summary>
    public class UIPooling : IInitializable, IDisposable, IUIPooling {
        private readonly Dictionary<string, GameObjectPool> _objectPools = new(StringComparer.Ordinal);
        private readonly IScopeLocator                      _locator;

        public Transform Root { get; private set; }

        public IObjectResolver Resolver => _locator?.GetLastChildScope()?.Container;

        public UIPooling(
            IScopeLocator locator
        ) {
            _locator = locator;
        }

        public void Initialize() {
            var root = new GameObject("UIRoot");
            Root          = root.transform;
            Root.position = new Vector3(int.MaxValue, int.MaxValue, 0);
            UnityEngine.Object.DontDestroyOnLoad(root);
        }

        public GameObject Pop(string key, Transform parent = null, bool active = true, bool worldPositionStays = true) {
            if (_objectPools.TryGetValue(key, out var pool) == false) {
                pool = CreatePool(key);
            }

            var obj = pool.Pop();
            obj.SetActiveSafe(active);
            obj.transform.SetParent(parent, worldPositionStays);
            return obj;
        }

        private GameObjectPool CreatePool(string key) {
            var pool = new GameObjectPool(this, key);
            _objectPools.Add(key, pool);
            return pool;
        }
        public bool Push(GameObject obj) {
            obj.SetActiveSafe(false);

            if (obj.TryGetComponent<IPoolMember>(out var member) == false) {
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

        public bool Exists(string key) {
            return _objectPools.ContainsKey(key);
        }

        public void Clear() {
            foreach (var pool in _objectPools) {
                pool.Value.Dispose();
            }

            _objectPools.Clear();
        }

        private void Release() {
            Clear();

            if (Root) {
                Object.Destroy(Root);
            }
        }

        public void Dispose() {
            Release();
        }
    }
}