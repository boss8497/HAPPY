using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using CharacterInfo = Script.GameInfo.Info.Character.CharacterInfo;

namespace Script.GameInfo.Table {
    
    [System.Serializable]
    [CreateAssetMenu(fileName = "CharacterTable", menuName = "Data/CharacterTable")]
    public class CharacterTable : TableBase {
        public override InfoBase[] Infos => CharacterInfos.OfType<InfoBase>().ToArray();
        public override Type ElementType {
            get {
                _type ??= typeof(CharacterInfo);
                return _type;
            }
        }

        public override T[] GetCollection<T>() {
            if(CharacterInfos is T[] collection)
                return collection;
            return Array.Empty<T>();
        }

        [NonSerialized]
        private Type _type;

        [field: SerializeField]
        public          CharacterInfo[] CharacterInfos = Array.Empty<CharacterInfo>();
    }
}