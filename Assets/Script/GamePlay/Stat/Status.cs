using System;
using System.Collections.Generic;
using System.Linq;
using Script.GameInfo.Info.Enum;
using Script.GameInfo.Info.Stat;
using Script.Utility.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Script.GamePlay.Stat {
    [System.Serializable]
    public class Status : IClassPool {
        //같은 StatusInfo가 들어올 수 있기 때문에 List로 관리 한다.
        [ShowInInspector]
        private List<StatusInfo> _originalStatus;

        private double[] _cacheValue        = new double[(int)StatType.Max];
        private double[] _cachePercentValue = new double[(int)StatType.Max];


        //Value * Percent 합한 스텟
        [ShowInInspector]
        private double[] _calcValue = new double[(int)StatType.Max];


        //Property
        public double Hp   => _calcValue[(int)StatType.Hp];
        public double Mp   => _calcValue[(int)StatType.Mp];
        public double Atk  => _calcValue[(int)StatType.Atk];
        public double Def  => _calcValue[(int)StatType.Def];
        public double Spd  => _calcValue[(int)StatType.Spd];
        public double Jump => _calcValue[(int)StatType.Jump];


        public void Add(StatusInfo statusInfo) {
            _originalStatus.Add(statusInfo);

            var typeListPool = ListPool.Get<StatType>();
            foreach (var stat in statusInfo.status) {
                if (stat.type == StatType.Max) continue;
                
                var index = (int)stat.type;
                if (stat.isPercent) {
                    _cachePercentValue[index] += stat.Calc();
                }
                else {
                    _cacheValue[index] += stat.Calc();
                }

                if (typeListPool.Exists(a => a == stat.type) == false) {
                    typeListPool.Add(stat.type);
                }
            }

            UpdateAt(typeListPool);
            ListPool.Return(typeListPool);
        }

        public void Remove(StatusInfo statusInfo) {
            Remove(statusInfo.UID);
        }

        public void Remove(int uid) {
            var index = _originalStatus.FindLastIndex(r => r.UID == uid);
            if (index >= 0) {
                var removeStatusInfo = _originalStatus[index];
                _originalStatus.RemoveAt(index);

                var typeListPool = ListPool.Get<StatType>();
                foreach (var stat in removeStatusInfo.status) {
                    if (stat.type == StatType.Max) continue;
                
                    var statIndex = (int)stat.type;
                    if (stat.isPercent) {
                        _cachePercentValue[statIndex] -= stat.Calc();
                    }
                    else {
                        _cacheValue[statIndex] -= stat.Calc();
                    }
                    
                    if (typeListPool.Exists(a => a == stat.type) == false) {
                        typeListPool.Add(stat.type);
                    }
                }
                
                UpdateAt(typeListPool);
                ListPool.Return(typeListPool);
            }
        }

        public void UpdateAt(List<StatType>  typeList) {
            foreach (var stat in typeList) {
                var index =  (int)stat;
                _calcValue[index] = _cacheValue[index] * _cachePercentValue[index];
            }
        }
        
        public void Update() {
            for (int i = 0; i < (int)StatType.Max; i++) {
                _calcValue[i] = _cacheValue[i] * _cachePercentValue[i];
            }
        }

        public void Release() {
            _originalStatus.Clear();
            for (int i = 0; i < (int)StatType.Max; i++) {
                _calcValue[i] = _cacheValue[i] = _cachePercentValue[i] = 0;
            }

            ListPool.Return<StatusInfo>(_originalStatus);
            _originalStatus = null;
        }

        public void OnRent() {
            _originalStatus = ListPool.Get<StatusInfo>();
        }
        public void OnReturn() {
            Release();
        }
    }
}