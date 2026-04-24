using System.Collections.Generic;
using Script.GameInfo.Info.Stat;

namespace Script.Buff {
    public interface IBuffOwner {
        void AddStatus(List<StatusInfo> infos);
        void RemoveStatus(IEnumerable<StatusInfo> infos);
    }
}