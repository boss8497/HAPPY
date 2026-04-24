using System.Linq;
using Script.GameInfo.Info;
using Script.GameInfo.Info.Stat;
using Script.GameInfo.Table;
using Script.Utility.Runtime;

namespace Script.Buff {
    public class Buff : IBuff, IClassPool {
        private BuffInfo _buffInfo;
        private UmBuff   _umBuff;

        public long         Uid         { get; set; }
        public StatusInfo[] StatusInfos { get; private set; }

        public void Initialize(BuffInfo info, long uid) {
            _buffInfo   = info;
            Uid         = uid;
            StatusInfos = _buffInfo.statusUid.Select(i => GameInfoManager.Instance.Get<StatusInfo>(i)).ToArray();
        }

        public void OnRent() { }

        public void OnReturn() {
            Uid         = -1;
            _buffInfo   = null;
            StatusInfos = null;
        }
    }
}