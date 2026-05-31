using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Script.Buff;
using Script.GameInfo.Info.Stat;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Character {
    public partial class Character : IBuffOwner {
        private BuffSystem _buffSystem;
        

        private void InitializeBuff() {
            _buffSystem = ClassPool.Get<BuffSystem>();
            _buffSystem.Initialize(this, GameTimer);
        }

        private void ReleaseBuff() {
            if (_buffSystem != null) {
                ClassPool.Release(_buffSystem);
            }
            _buffSystem = null;
        }

        private void ApplyBuff(int[] buffUids) {
            _buffSystem.AddBuffs(buffUids);
        }

        public void AddStatus(List<StatusInfo> infos) {
            foreach (var statusInfo in infos) {
                _status.Add(statusInfo);
            }
            UpdateStatus();
        }

        public void RemoveStatus(IEnumerable<StatusInfo> infos) {
            foreach (var statusInfo in infos) {
                _status.Remove(statusInfo);
            }
            UpdateStatus();
        }
    }
}