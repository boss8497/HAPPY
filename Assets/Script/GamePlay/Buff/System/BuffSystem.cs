using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.GameInfo.Info.Stat;
using Script.GameInfo.Table;
using Script.GameTimer;
using Script.Utility.Runtime;
using Unity.Collections;

namespace Script.Buff {
    /// <summary>
    /// 한 객체의 Buff System
    /// 모든 버프를 컨트롤 하지 않고 한 객체 마다 Buff System을 따로 배치
    /// </summary>
    public class BuffSystem : IBuffSystem, IClassPool, IDisposable {
        // 오브젝트를 가지고 있는 Owner에 대한 버프 uid
        // 서버에서 사용한다면 Owner uid + buff uid  조합해서 사용할 것
        private readonly Queue<long> _returnIndex = new();
        private          long        _uidIndexer;

        private IBuffOwner _owner;
        private IGameTimer _gameTimer;

        private List<Buff>     _buffs;
        private List<UmBuff> _umBuffs;

        private CancellationTokenSource _cts;


        public void Initialize(IBuffOwner owner, IGameTimer gameTimer) {
            // 글쌔 16개 이상 버프를 가지고 있을까..? 디버프도 생각해야되긴 한데 일단은 16
            // 너무 적은 숫자라서 Burst로 이득을볼 수 있을까? 흐음
            //_umBuffs     = new (16, Allocator.Persistent);
            _umBuffs   = ListPool.Get<UmBuff>();
            _owner     = owner;
            _gameTimer = gameTimer;
            _buffs     = ListPool.Get<Buff>();
        }

        private async UniTask Update(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                var removeBuffs = ListPool.Get<long>();
                var elapsed     = _gameTimer.Elapsed;

                for (int i = _umBuffs.Count - 1; i >= 0; i--) {
                    if (_umBuffs[i].endTime <= elapsed)
                        removeBuffs.Add(_umBuffs[i].buffUid);
                }

                if (removeBuffs.Count > 0) {
                    foreach (var buff in removeBuffs) {
                        RemoveBuff(buff);
                    }
                    removeBuffs.Clear();
                }
                ListPool.Return(removeBuffs);
                
                var isCancel = await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct).SuppressCancellationThrow();
                if (isCancel) {
                    break;
                }
            }
        }
        
        
        public void AddBuffs(int[] uids) {
            if ((uids?.Length ?? 0) <= 0) return;

            var statInfos = ListPool.Get<StatusInfo>();
            foreach (var uid in uids) {
                var buffInfo = GameInfoManager.Instance.Get<BuffInfo>(uid);
                AddBuff(buffInfo);
                statInfos.AddRange(buffInfo.statusUid.Select(i => GameInfoManager.Instance.Get<StatusInfo>(i)));
            }
            
            _owner.AddStatus(statInfos);
            statInfos.Clear();
            ListPool.Return(statInfos);
        }

        private void AddBuff(BuffInfo buffInfo) {
            if (buffInfo == null) return;
            var buff   = ClassPool.Get<Buff>();
            var newUid = NewUid();
            buff.Initialize(buffInfo, newUid);
            
            _buffs.Add(buff);
            _umBuffs.Add(new () {
                buffUid = newUid,
                endTime = buffInfo.time + _gameTimer.Elapsed
            });
            
            if (_cts == null) {
                _cts = new();
                Update(_cts.Token).Forget();
            }
        }

        public void RemoveBuff(long uid) {
            var buff = _buffs.Find(r => r.Uid == uid);
            if (buff == null) return;

            _buffs.Remove(buff);
            _umBuffs.RemoveSwapBack(r => r.buffUid == uid);
            _owner.RemoveStatus(buff.StatusInfos);
            
            ClassPool.Release(buff);

            if (_umBuffs.Count <= 0) {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private long NewUid() {
            return _returnIndex.Count <= 0 ? _uidIndexer++ : _returnIndex.Dequeue();
        }

        public void OnRent() {
        }

        public void OnReturn() {
            ListPool.Return(_buffs);
            ListPool.Return(_umBuffs);
        }

        public void Dispose() {
            if (_cts is { IsCancellationRequested: false }) {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            OnReturn();
        }
    }
}