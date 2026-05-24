using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using Script.GameInfo;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "RewardTable", menuName = "Data/Table/RewardTable")]
    public partial class RewardTable : TableBase {
        public override InfoBase[] Infos {
            get => RewardInfos.OfType<InfoBase>().ToArray();
            set {
                if (value == null) {
                    RewardInfos = Array.Empty<RewardInfo>();
                    return;
                }

                var typedInfos = value.OfType<RewardInfo>().ToArray();
                if (typedInfos.Length != value.Length) {
                    Debug.LogError($"모든 요소가 RewardInfo 타입이 아닙니다.");
                    return;
                }

                RewardInfos = typedInfos;
            }
        }

        public override Type ElementType {
            get {
                _type ??= typeof(RewardInfo);
                return _type;
            }
        }

        [NonSerialized]
        private Type _type;

        [SerializeReference]
        public RewardInfo[] RewardInfos = Array.Empty<RewardInfo>();

        public override T[] GetCollection<T>() {
            if (RewardInfos is T[] collection)
                return collection;

            return Array.Empty<T>();
        }
    }
}
