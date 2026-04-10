using Script.GamePlay.Pool;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        
        private void InitializePool() {
            _players       = ListPool.Get<Character.Character>();
            _enemies       = ListPool.Get<Character.Character>();
        }

        private void ReleasePool() {
            if (_players != null) {
                foreach (var character in _players) {
                    character.Release();
                    
                    // StagePooling이 Null인거는 Stage Scene이 파괴 됐다는 것과 같기 때문에
                    // 따로 Object.Destroy를 호출 하지 않아도
                    // Scene이 파괴 되면서 Object 자동 파괴.
                    if(StagePooling != null) {
                        StagePooling.Push(character.gameObject);
                    }
                }

                _players.Clear();
                ListPool.Return(_players);
            }
            
            
            if (_enemies != null) {
                foreach (var character in _enemies) {
                    character.Release();
                    if(StagePooling != null) {
                        StagePooling.Push(character.gameObject);
                    }
                }
                _enemies.Clear();
                ListPool.Return(_enemies);
            }
        }
    }
}