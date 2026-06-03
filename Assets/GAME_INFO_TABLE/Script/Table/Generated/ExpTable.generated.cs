using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using Script.GameInfo;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "ExpTable", menuName = "Data/Table/ExpTable")]
    public partial class ExpTable : TableBase {
        public override InfoBase[] Infos {
            get => ExpInfos.OfType<InfoBase>().ToArray();
            set {
                if (value == null) {
                    ExpInfos = Array.Empty<ExpInfo>();
                    return;
                }

                var typedInfos = value.OfType<ExpInfo>().ToArray();
                if (typedInfos.Length != value.Length) {
                    Debug.LogError($"모든 요소가 ExpInfo 타입이 아닙니다.");
                    return;
                }

                ExpInfos = typedInfos;
            }
        }

        public override Type ElementType {
            get {
                _type ??= typeof(ExpInfo);
                return _type;
            }
        }

        [NonSerialized]
        private Type _type;

        [SerializeReference]
        public ExpInfo[] ExpInfos = Array.Empty<ExpInfo>();

        public override T[] GetCollection<T>() {
            if (ExpInfos is T[] collection)
                return collection;

            return Array.Empty<T>();
        }
    }
}
