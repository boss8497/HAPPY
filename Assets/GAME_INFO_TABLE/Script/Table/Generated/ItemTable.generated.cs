using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using Script.GameInfo.Item;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "ItemTable", menuName = "Data/Table/ItemTable")]
    public partial class ItemTable : TableBase {
        public override InfoBase[] Infos {
            get => ItemInfos.OfType<InfoBase>().ToArray();
            set {
                if (value == null) {
                    ItemInfos = Array.Empty<ItemInfo>();
                    return;
                }

                var typedInfos = value.OfType<ItemInfo>().ToArray();
                if (typedInfos.Length != value.Length) {
                    Debug.LogError($"모든 요소가 ItemInfo 타입이 아닙니다.");
                    return;
                }

                ItemInfos = typedInfos;
            }
        }

        public override Type ElementType {
            get {
                _type ??= typeof(ItemInfo);
                return _type;
            }
        }

        [NonSerialized]
        private Type _type;

        [SerializeReference]
        public ItemInfo[] ItemInfos = Array.Empty<ItemInfo>();

        public override T[] GetCollection<T>() {
            if (ItemInfos is T[] collection)
                return collection;

            return Array.Empty<T>();
        }
    }
}
