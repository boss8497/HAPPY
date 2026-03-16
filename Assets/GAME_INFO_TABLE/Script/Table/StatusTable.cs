using System;
using System.Linq;
using Script.GameInfo.Base;
using Script.GameInfo.Info.Stat;
using UnityEngine;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "StatusTable", menuName = "Data/Table/StatusTable")]
    public class StatusTable : TableBase {
        public override InfoBase[] Infos {
            get => StatusInfos.OfType<InfoBase>().ToArray();
            set {
                if (value == null) {
                    StatusInfos = Array.Empty<StatusInfo>();
                    return;
                }

                var behaviourInfos = value.OfType<StatusInfo>().ToArray();
                if (behaviourInfos.Length != value.Length) {
                    Debug.LogError("모든 요소가 BehaviourInfo 타입이 아닙니다.");
                    return;
                }

                StatusInfos = behaviourInfos;
            }
        }

        public override Type ElementType {
            get {
                _type ??= typeof(StatusInfo);
                return _type;
            }
        }

        [NonSerialized]
        private Type _type;

        public StatusInfo[] StatusInfos = Array.Empty<StatusInfo>();

        public override T[] GetCollection<T>() {
            if (StatusInfos is T[] collection)
                return collection;
            return Array.Empty<T>();
        }
    }
}