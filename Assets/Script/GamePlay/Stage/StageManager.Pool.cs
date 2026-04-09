using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private void InitializePool() {
            _players       = ListPool.Get<Character.Character>();
            _playerObjects = ListPool.Get<GameObject>();
            
            _enemies       = ListPool.Get<Character.Character>();
            _enemyObjects = ListPool.Get<GameObject>();
        }

        private void ReleasePool() {
            if (_players != null) {
                foreach (var character in _players) {
                    character.Release();
                }

                _players.Clear();
                ListPool.Return(_players);
            }

            if (_playerObjects != null) {
                foreach (var characterObject in _playerObjects) {
                    Object.Destroy(characterObject);
                }

                _playerObjects.Clear();
                ListPool.Return(_playerObjects);
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