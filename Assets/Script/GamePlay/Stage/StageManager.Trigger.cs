using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Script.GameInfo.Dungeon;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private List<ClientTriggerBase> _clientTriggers;

        private void InitializeTrigger() {
            _clientTriggers = ListPool.Get<ClientTriggerBase>();
            CreateClientTrigger();
        }

        private void CreateClientTrigger() {
            _clientTriggers.AddRange(PhaseInfo.CurrentValue.triggers.Select(trigger => {
                var clientTrigger = TriggerFactory.Create(trigger);
                clientTrigger.Initialize(this);
                return clientTrigger;
            }));
        }

        private void ReleaseTrigger() {
            foreach (var trigger in _clientTriggers) {
                trigger.Release();
            }
            _clientTriggers.Clear();
            ListPool.Return(_clientTriggers);
        }

        private ClientTriggerBase OnTriggerCheck() {
            return _clientTriggers.FirstOrDefault(r => r.OnTrigger());
        }

        private bool OnTrigger(ClientTriggerBase trigger) {
            var loopStop = false;
            switch (trigger.Type) {
                case TriggerType.Clear:
                    AddState(StageState.Clear);
                    loopStop = true;
                    break;
                
                case TriggerType.Fail:
                    AddState(StageState.Fail);
                    loopStop = true;
                    break;
            }

            return loopStop;
        }
    }
}