using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using Script.GameInfo.Character;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "BehaviourTable", menuName = "Data/Table/BehaviourTable")]
    public partial class BehaviourTable : TableBase {
        public override InfoBase[] Infos {
            get => Behaviours.OfType<InfoBase>().ToArray();
            set {
                if (value == null) {
                    Behaviours = Array.Empty<BehaviourInfo>();
                    return;
                }

                var behaviourInfos = value.OfType<BehaviourInfo>().ToArray();
                if (behaviourInfos.Length != value.Length) {
                    Debug.LogError("모든 요소가 BehaviourInfo 타입이 아닙니다.");
                    return;
                }

                Behaviours = behaviourInfos;
            }
        }

        public override Type ElementType {
            get {
                _type ??= typeof(BehaviourInfo);
                return _type;
            }
        }

        [NonSerialized]
        private Type _type;

        [SerializeReference]
        public BehaviourInfo[] Behaviours = Array.Empty<BehaviourInfo>();


        public override T[] GetCollection<T>() {
            if (Behaviours is T[] collection)
                return collection;
            return Array.Empty<T>();
        }
    }
}