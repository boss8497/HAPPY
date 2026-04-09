using System.Collections.Generic;
using System.Linq;
using Script.GameInfo.Dungeon;
using Script.Utility.Runtime;

namespace Script.GamePlay.Stage {
    public class ClientPlayerDieTrigger : ClientTriggerBase {
        private IStageManager             _stageManager;
        private List<Character.Character> _players;

        private bool _isInitializedStageManager = false;

        private bool _initialized = false;

        public ClientPlayerDieTrigger(TriggerBase trigger) : base(trigger) { }

        public override void Initialize(IStageManager stageManager) {
            _stageManager              = stageManager;
            _isInitializedStageManager = _stageManager.Initialized?.CurrentValue ?? false;

            _players = ListPool.Get<Character.Character>();

            if (_stageManager.Initialized?.CurrentValue ?? false) {
                _players.AddRange(_stageManager.Players);
                _initialized = true;
            }
        }

        public override bool OnTrigger() {
            if (_initialized == false) {
                if (_stageManager.Initialized?.CurrentValue ?? false) {
                    _players.AddRange(_stageManager.Players);
                    _initialized = true;
                }
                else {
                    return false;
                }
            }
            return _players.All(r => (r.Die?.CurrentValue ?? false));
        }

        public override void Release() {
            _players.Clear();
            ListPool.Return(_players);
        }
    }
}