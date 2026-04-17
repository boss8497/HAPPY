using System.Collections.Generic;
using Script.GamePlay.Buff;
using Script.Utility.Runtime;

namespace Script.GamePlay.Character {
    public partial class Character {
        private List<Buff.Buff> _buffs;

        private void InitializeBuff() {
            _buffs ??= ListPool.Get<Buff.Buff>();
        }

        private void ApplyBuff(int[] buffUids) {
            if ((buffUids?.Length ?? 0) <= 0) return;

            foreach (var buffUid in buffUids) {
                var buff = ClassPool.Get<Buff.Buff>();
                _buffs.Add(buff);
                buff.Initialize(this, buffUid);
                buff.StartBuff();
                foreach (var statusInfo in buff.StatusInfos) {
                    _status.Add(statusInfo);
                }
            }

            UpdateStatus();
        }

        public void RemoveBuff(IBuff iBuff) {
            if (iBuff is Buff.Buff buff) {
                _buffs.Remove(buff);
                foreach (var statusInfo in buff.StatusInfos) {
                    _status.Remove(statusInfo);
                }

                ClassPool.Release(buff);
            }
            
            UpdateStatus();
        }

        private void ReleaseBuff() {
            if (_buffs != null) {
                foreach (var buff in _buffs) {
                    ClassPool.Release(buff);
                }

                _buffs.Clear();
                ListPool.Return(_buffs);
                _buffs = null;
            }
        }
    }
}