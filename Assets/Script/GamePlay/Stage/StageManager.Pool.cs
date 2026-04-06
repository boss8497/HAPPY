using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private void InitializePool() {
            _characters       = ListPool.Get<Character.Character>();
            _characterObjects = ListPool.Get<GameObject>();
            
            _enemies       = ListPool.Get<Character.Character>();
            _enemyObjects = ListPool.Get<GameObject>();
        }

        private void ReleasePool() {
            if (_characters != null) {
                foreach (var character in _characters) {
                    character.Release();
                }

                _characters.Clear();
                ListPool.Return(_characters);
            }

            if (_characterObjects != null) {
                foreach (var characterObject in _characterObjects) {
                    Object.Destroy(characterObject);
                }

                _characterObjects.Clear();
                ListPool.Return(_characterObjects);
            }
            
            
            
            if (_enemies != null) {
                foreach (var character in _enemies) {
                    character.Release();
                }
                _enemies.Clear();
                ListPool.Return(_enemies);
            }

            if (_enemyObjects != null) {
                foreach (var characterObject in _enemyObjects) {
                    Object.Destroy(characterObject);
                }
                _enemyObjects.Clear();
                ListPool.Return(_enemyObjects);
            }
        }
    }
}