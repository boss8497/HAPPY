using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using CharacterInfo = Script.GameInfo.Info.Character.CharacterInfo;

namespace Script.GameInfo.Table {
    
    [System.Serializable]
    [CreateAssetMenu(fileName = "CharacterTable", menuName = "Data/CharacterTable")]
    public class CharacterTable : TableBase {
        public override InfoBase[] Infos   => CharacterInfos.OfType<InfoBase>().ToArray();

        [field: SerializeField]
        public          CharacterInfo[] CharacterInfos = Array.Empty<CharacterInfo>();
    }
}