using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using Script.GameInfo.Info.Dungeon;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "DungeonTable", menuName = "Data/Table/DungeonTable")]
    public partial class DungeonTable : TableBase {
        public override InfoBase[] Infos {
            get => DungeonInfos.OfType<InfoBase>().ToArray();
            set {
                if (value == null) {
                    DungeonInfos = Array.Empty<DungeonInfo>();
                    return;
                }

                var typedInfos = value.OfType<DungeonInfo>().ToArray();
                if (typedInfos.Length != value.Length) {
                    Debug.LogError($"모든 요소가 DungeonInfo 타입이 아닙니다.");
                    return;
                }

                DungeonInfos = typedInfos;
            }
        }

        public override Type ElementType {
            get {
                _type ??= typeof(DungeonInfo);
                return _type;
            }
        }

        [NonSerialized]
        private Type _type;

        [SerializeReference]
        public DungeonInfo[] DungeonInfos = Array.Empty<DungeonInfo>();

        public override T[] GetCollection<T>() {
            if (DungeonInfos is T[] collection)
                return collection;

            return Array.Empty<T>();
        }
    }
}
