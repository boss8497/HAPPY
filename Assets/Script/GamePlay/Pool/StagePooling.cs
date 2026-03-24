using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace Script.GamePlay.Pool {
    public class StagePooling : MonoBehaviour, IPooling {
        private readonly Dictionary<string, ObjectPoolSystem> _objectPools = new(StringComparer.Ordinal);

        public bool Exists(string key) {
            return _objectPools.ContainsKey(key);
        }

        public GameObject Pop(string key) {
            if (_objectPools.TryGetValue(key, out var pool) == false) {
                pool = CreatePool(key);
            }
            return pool.Pop();
        }

        private ObjectPoolSystem CreatePool(string key) {
            var pool = new ObjectPoolSystem(this, key);
            _objectPools.Add(key, pool);
            return pool;
        }

        public bool Push(GameObject obj) {
            if (obj.TryGetComponent<PoolMember>(out var member) == false) {
                Destroy(obj);
                return false;
            }
            
            if (_objectPools.TryGetValue(member.Key, out var pool)) {
                pool.Push(obj);
                return true;
            }
            
            Destroy(obj);
            return false;
        }
        
    }
}