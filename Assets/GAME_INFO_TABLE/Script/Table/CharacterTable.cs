using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using CharacterInfo = Script.GameInfo.Info.Character.CharacterInfo;

namespace Script.GameInfo.Table {
    
    [System.Serializable]
    [CreateAssetMenu(fileName = "CharacterTable", menuName = "Data/Table/CharacterTable")]
    public class CharacterTable : TableBase {
        public override InfoBase[] Infos {
            get => CharacterInfos.OfType<InfoBase>().ToArray();
            set {
                if (value == null) {
                    CharacterInfos = Array.Empty<CharacterInfo>();
                    return;
                }

                var behaviourInfos = value.OfType<CharacterInfo>().ToArray();
                if (behaviourInfos.Length != value.Length) {
                    Debug.LogError("모든 요소가 BehaviourInfo 타입이 아닙니다.");
                    return;
                }

                CharacterInfos = behaviourInfos;
            }
        }
        public override Type ElementType {
            get {
                _type ??= typeof(CharacterInfo);
                return _type;
            }
        }

        [NonSerialized]
        private Type _type;

        [field: SerializeField]
        public          CharacterInfo[] CharacterInfos = Array.Empty<CharacterInfo>();
        

        public override T[] GetCollection<T>() {
            if(CharacterInfos is T[] collection)
                return collection;
            return Array.Empty<T>();
        }
    }
}