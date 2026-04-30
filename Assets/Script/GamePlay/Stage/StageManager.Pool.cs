using Script.GamePlay.Pool;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private void InitializePool() {
            _players        ??= ListPool.Get<Character.ICharacter>();
            _enemies        ??= ListPool.Get<Character.ICharacter>();
            _RemoveEnemies ??= ListPool.Get<Character.ICharacter>();
        }

        private void ReleasePool() {
            ReleaseCharacter();

            ListPool.Return(_players);
            ListPool.Return(_enemies);
            ListPool.Return(_RemoveEnemies);
        }
    }
}