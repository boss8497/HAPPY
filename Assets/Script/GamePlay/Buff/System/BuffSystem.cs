using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.GameInfo.Info.Enum;
using Script.GameInfo.Info.Stat;
using Script.GameInfo.Table;
using Script.GamePlay.Camera;
using Script.GameTimer;
using Script.Utility.Runtime;
using Unity.Collections;
using UnityEngine;

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

        private IBuffOwner      _owner;
        private IGameTimer      _gameTimer;
        private ICameraControls _cameraControls;

        private List<Buff>   _buffs;
        private List<UmBuff> _umBuffs;

        private CancellationTokenSource _cts;

        public bool IsInitialize { get; private set; } = false;


        /// <param name="cameraControls">
        /// Speed 버프 fade에 맞춰 카메라 연출(Zoom/Offset)을 같이 재생할 대상. Player 소유 BuffSystem에만 전달한다 (null이면 카메라 연출 없음).
        /// </param>
        public void Initialize(IBuffOwner owner, IGameTimer gameTimer, ICameraControls cameraControls = null) {
            // 글쌔 16개 이상 버프를 가지고 있을까..? 디버프도 생각해야되긴 한데 일단은 16
            // 너무 적은 숫자라서 Burst로 이득을볼 수 있을까? 흐음
            //_umBuffs     = new (16, Allocator.Persistent);
            _owner          = owner;
            _gameTimer      = gameTimer;
            _cameraControls = cameraControls;
            _buffs          = ListPool.Get<Buff>();
            _umBuffs        = ListPool.Get<UmBuff>();
            IsInitialize    = true;
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

                if (_umBuffs.Count > 0) {
                    NotifySpeedFade(elapsed);
                }

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

            // AddStatus 이후 즉시 fade 상태 적용 (새 버프 startTime 기준 factor ≈ 0)
            NotifySpeedFade(_gameTimer.Elapsed);
        }

        private void AddBuff(BuffInfo buffInfo) {
            if (buffInfo == null) return;
            var buff      = ClassPool.Get<Buff>();
            var newUid    = NewUid();
            var startTime = _gameTimer.Elapsed;
            buff.Initialize(buffInfo, newUid);

            _buffs ??= ListPool.Get<Buff>();
            _umBuffs ??= ListPool.Get<UmBuff>();

            _buffs.Add(buff);
            _umBuffs.Add(new UmBuff {
                buffUid         = newUid,
                startTime       = startTime,
                endTime         = startTime + buffInfo.time,
                fadeInDuration  = buffInfo.fadeInTime,
                fadeOutDuration = buffInfo.fadeOutTime,
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

            // RemoveStatus 이전에 남은 버프 기준으로 fade 업데이트
            // (RemoveStatus 내부에서 UpdateRunningStatus가 호출될 때 올바른 bonus 값 사용)
            NotifySpeedFade(_gameTimer.Elapsed);

            _owner.RemoveStatus(buff.StatusInfos);
            ClassPool.Release(buff);

            if (_umBuffs.Count <= 0) {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        // 현재 버프들의 Spd 기여분과 fade factor를 계산해 owner와 카메라에 전달
        private void NotifySpeedFade(float elapsed) {
            if (_umBuffs == null || _umBuffs.Count == 0) {
                _owner.OnBuffSpeedFade(0f, 1f);
                _cameraControls?.SetSpeedBoostFade(0f);
                return;
            }

            float totalSpdBonus    = 0f;
            float effectiveSpdBonus = 0f;

            foreach (var umBuff in _umBuffs) {
                var buff    = umBuff;
                var buffObj = _buffs.Find(b => b.Uid == buff.buffUid);
                if (buffObj == null) continue;

                float spdBonus = CalcSpdBonus(buffObj.StatusInfos);
                if (spdBonus <= 0f) continue;

                float factor = CalcFadeFactor(in umBuff, elapsed);
                totalSpdBonus     += spdBonus;
                effectiveSpdBonus += spdBonus * factor;
            }

            // totalSpdBonus가 0이면(Spd 버프가 하나도 없으면) factor는 의미가 없으므로 카메라는 평상시(0)로 취급한다.
            float overallFactor = totalSpdBonus > 0f ? effectiveSpdBonus / totalSpdBonus : 1f;
            _owner.OnBuffSpeedFade(totalSpdBonus, overallFactor);
            _cameraControls?.SetSpeedBoostFade(totalSpdBonus > 0f ? overallFactor : 0f);
        }

        private static float CalcSpdBonus(StatusInfo[] statusInfos) {
            float total = 0f;
            foreach (var info in statusInfos) {
                foreach (var stat in info.status) {
                    if (stat.type == StatType.Spd && !stat.isPercent)
                        total += (float)stat.Calc();
                }
            }
            return total;
        }

        private static float CalcFadeFactor(in UmBuff umBuff, float elapsed) {
            float timePassed = elapsed - umBuff.startTime;
            float timeLeft   = umBuff.endTime - elapsed;

            if (umBuff.fadeInDuration > 0f && timePassed < umBuff.fadeInDuration)
                return Mathf.Clamp01(timePassed / umBuff.fadeInDuration);

            if (umBuff.fadeOutDuration > 0f && timeLeft < umBuff.fadeOutDuration)
                return Mathf.Clamp01(timeLeft / umBuff.fadeOutDuration);

            return 1f;
        }

        private long NewUid() {
            return _returnIndex.Count <= 0 ? _uidIndexer++ : _returnIndex.Dequeue();
        }

        public void OnRent() {
            IsInitialize = false;
        }

        public void OnReturn() {
            Release();
            IsInitialize = false;
        }

        public void Release() {
            if (_cts is { IsCancellationRequested: false }) {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (_buffs != null) {
                foreach (var buff in _buffs) {
                    ClassPool.Release(buff);
                }

                _buffs.Clear();
                ListPool.Return(_buffs);
            }

            if (_umBuffs != null) {
                _umBuffs.Clear();
                ListPool.Return(_umBuffs);
            }

            // 부스터 도중 owner가 해제되는 경우(ReStart 등) 카메라 연출이 확대된 채로 남지 않도록 원상 복구한다.
            _cameraControls?.SetSpeedBoostFade(0f);

            _umBuffs        = null;
            _owner          = null;
            _gameTimer      = null;
            _cameraControls = null;
            _buffs          = null;
        }

        public void Dispose() {
            Release();
        }
    }
}