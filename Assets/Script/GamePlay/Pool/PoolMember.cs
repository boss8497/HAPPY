using System;
using UnityEngine;

namespace Script.GamePlay.Pool {
    public class PoolMember : MonoBehaviour {
        private IObjectPoolSystem  _objectPoolSystem;
        
        public string Key => _objectPoolSystem.Key;
        
        public void Set(IObjectPoolSystem objectPoolSystem) {
            _objectPoolSystem = objectPoolSystem;
        }
    }
}