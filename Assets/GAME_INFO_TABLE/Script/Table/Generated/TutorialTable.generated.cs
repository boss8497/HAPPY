using System;
using System.Linq;
using Script.GameInfo.Base;
using UnityEngine;
using Script.GameInfo.Info;

namespace Script.GameInfo.Table {
    [System.Serializable]
    [CreateAssetMenu(fileName = "TutorialTable", menuName = "Data/Table/TutorialTable")]
    public partial class TutorialTable : TableBase {
        public override InfoBase[] Infos {
            get => TutorialInfos.OfType<InfoBase>().ToArray();
            set {
                if (value == null) {
                    TutorialInfos = Array.Empty<TutorialInfo>();
                    return;
                }

                var typedInfos = value.OfType<TutorialInfo>().ToArray();
                if (typedInfos.Length != value.Length) {
                    Debug.LogError($"모든 요소가 TutorialInfo 타입이 아닙니다.");
                    return;
                }

                TutorialInfos = typedInfos;
            }
        }

        public override Type ElementType {
            get {
                _type ??= typeof(TutorialInfo);
                return _type;
            }
        }

        [NonSerialized]
        private Type _type;

        [SerializeReference]
        public TutorialInfo[] TutorialInfos = Array.Empty<TutorialInfo>();

        public override T[] GetCollection<T>() {
            if (TutorialInfos is T[] collection)
                return collection;

            return Array.Empty<T>();
        }
    }
}
