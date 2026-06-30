using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Script.Buff;
using Script.GameInfo.Info.Stat;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Character {
    public partial class Character : IBuffOwner {
        private BuffSystem _buffSystem;

        // BuffSystem에서 전달받은 버프 Speed 보너스와 현재 fade 진행도
        private float _buffSpdBonus = 0f;
        private float _buffSpdFade  = 1f;

        private void InitializeBuff() {
            _buffSystem   = ClassPool.Get<BuffSystem>();
            _buffSpdBonus = 0f;
            _buffSpdFade  = 1f;
            _buffSystem.Initialize(this, GameTimer);
        }

        private void ReleaseBuff() {
            if (_buffSystem != null) {
                ClassPool.Release(_buffSystem);
            }
            _buffSystem   = null;
            _buffSpdBonus = 0f;
            _buffSpdFade  = 1f;
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

        // BuffSystem에서 매 프레임 호출 — ECS 속도를 점진적으로 보정
        public void OnBuffSpeedFade(float totalSpdBonus, float fadeFactor) {
            _buffSpdBonus = totalSpdBonus;
            _buffSpdFade  = fadeFactor;
            UpdateRunningStatus();
        }
    }
}