using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using Script.GameInfo.Info.Character.Behaviour;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "BehaviourTable", menuName = "Data/BehaviourTable")]
    public class BehaviourTable : TableBase{
        public override InfoBase[] Infos       => Behaviours.OfType<InfoBase>().ToArray();

        public override Type ElementType {
            get {
                _type ??= typeof(BehaviourInfo); 
                return _type;
            }
        }

        private Type _type;

        [SerializeReference]
        public BehaviourInfo[] Behaviours = Array.Empty<BehaviourInfo>();
    }
}