using System;
using UnityEngine;

namespace Script.GameInfo.Base {
    [System.Serializable]
    public abstract class TableBase : ScriptableObject {
        public abstract InfoBase[] Infos { get; set; }

        public abstract Type ElementType { get; }

        public abstract T[] GetCollection<T>() where T : InfoBase;

        public void Upsert<T>(T newInfo) where T : InfoBase {
            if (typeof(T) != ElementType) {
                Debug.LogError($"데이터 타입이 다릅니다. 기대되는 타입: {ElementType}, 전달된 타입: {typeof(T)}");
                return;
            }

            var oldInfos = Infos;
            var index    = Array.FindIndex(oldInfos, x => x != null && x.UID == newInfo.UID);

            if (index > 0) {
                oldInfos[index] = newInfo;
            }
            else {
                Array.Resize(ref oldInfos, oldInfos.Length + 1);
                newInfo.UID  = oldInfos.Length;
                oldInfos[^1] = newInfo;
            }

            Infos = oldInfos;
        }

        public bool Remove(int uid) {
            var oldInfos = Infos;
            if (oldInfos == null || oldInfos.Length == 0)
                return false;

            var index = Array.FindIndex(oldInfos, x => x != null && x.UID == uid);
            if (index < 0)
                return false;

            var newInfos = new InfoBase[oldInfos.Length - 1];

            if (index > 0)
                Array.Copy(oldInfos, 0, newInfos, 0, index);

            if (index < oldInfos.Length - 1)
                Array.Copy(oldInfos, index + 1, newInfos, index, oldInfos.Length - index - 1);

            Infos = newInfos;
            return true;
        }
    }
}