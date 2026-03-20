using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using Script.GameInfo.Dungeon;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "PhaseTable", menuName = "Data/Table/PhaseTable")]
    public partial class PhaseTable : TableBase {
        public override InfoBase[] Infos {
            get => PhaseInfos.OfType<InfoBase>().ToArray();
            set {
                if (value == null) {
                    PhaseInfos = Array.Empty<PhaseInfo>();
                    return;
                }

                var typedInfos = value.OfType<PhaseInfo>().ToArray();
                if (typedInfos.Length != value.Length) {
                    Debug.LogError($"모든 요소가 PhaseInfo 타입이 아닙니다.");
                    return;
                }

                PhaseInfos = typedInfos;
            }
        }

        public override Type ElementType {
            get {
                _type ??= typeof(PhaseInfo);
                return _type;
            }
        }

        [NonSerialized]
        private Type _type;

        [SerializeReference]
        public PhaseInfo[] PhaseInfos = Array.Empty<PhaseInfo>();

        public override T[] GetCollection<T>() {
            if (PhaseInfos is T[] collection)
                return collection;

            return Array.Empty<T>();
        }
    }
}
