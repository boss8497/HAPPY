using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using Script.GameInfo.Info;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "BuffTable", menuName = "Data/Table/BuffTable")]
    public partial class BuffTable : TableBase {
        public override InfoBase[] Infos {
            get => BuffInfos.OfType<InfoBase>().ToArray();
            set {
                if (value == null) {
                    BuffInfos = Array.Empty<BuffInfo>();
                    return;
                }

                var typedInfos = value.OfType<BuffInfo>().ToArray();
                if (typedInfos.Length != value.Length) {
                    Debug.LogError($"모든 요소가 BuffInfo 타입이 아닙니다.");
                    return;
                }

                BuffInfos = typedInfos;
            }
        }

        public override Type ElementType {
            get {
                _type ??= typeof(BuffInfo);
                return _type;
            }
        }

        [NonSerialized]
        private Type _type;

        [SerializeReference]
        public BuffInfo[] BuffInfos = Array.Empty<BuffInfo>();

        public override T[] GetCollection<T>() {
            if (BuffInfos is T[] collection)
                return collection;

            return Array.Empty<T>();
        }
    }
}
