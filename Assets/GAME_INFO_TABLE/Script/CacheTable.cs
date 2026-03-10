using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Script.GameInfo.Base;

namespace Script.GameInfo.Table {
    public class CacheTable {
        private string    _key;
        private TableBase _tableBase;

        private Dictionary<int, InfoBase> _infoByUid;
        private List<InfoBase>            _infos;

        public TableBase Table => _tableBase;

        public CacheTable(string key, TableBase tableBase) {
            _key       = key;
            _tableBase = tableBase;
            _infos     = _tableBase.Infos.ToList();
            _infoByUid = _tableBase.Infos.ToDictionary(i => i.UID, i => i);
        }

        public T Get<T>(int uid) where T : InfoBase {
            if (_infoByUid.TryGetValue(uid, out var info)) {
                return (T)info;
            }

            throw new KeyNotFoundException($"Info with UID {uid} not found in table {_key}");
        }

        [CanBeNull]
        public List<T> GetCollection<T>() where T : InfoBase {
            if (_infos is List<T> list) {
                return list;
            }

            return null;
        }

        public void Release() {
            _infoByUid.Clear();
            _infoByUid = null;
            _tableBase = null;
            _key       = null;
        }
    }
}