using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {


        private void InitializePool() {
            _characters       = ListPool.Get<Character.Character>();
            _characterObjects = ListPool.Get<GameObject>();
        }

        private void ReleasePool() {
            _characters.Clear();
            ListPool.Return(_characters);

            foreach (var characterObject in _characterObjects) {
                Object.Destroy(characterObject);
            }
            _characterObjects.Clear();
            ListPool.Return(_characterObjects);
        }
    }
}