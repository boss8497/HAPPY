using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using Script.GameInfo.Info.Character.Behaviour;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "BehaviourTable", menuName = "Data/BehaviourTable")]
    public class BehaviourTable : TableBase {
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