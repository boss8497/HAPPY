using System;
using UnityEngine;

// 반환이 만약 안됬다면 경고를 날려 줄 컴포넌트
// 아직 구현이 없음
namespace Script.GamePlay.Pool {
    public class PoolMember : MonoBehaviour, IPoolMember {
        private IGameObjectPool  _gameObjectPool;
        
        public string Key => _gameObjectPool.Key;
        
        public void Set(IGameObjectPool gameObjectPool) {
            _gameObjectPool = gameObjectPool;
        }
    }
}