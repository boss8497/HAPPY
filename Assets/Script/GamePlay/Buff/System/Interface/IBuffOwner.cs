using System.Collections.Generic;
using Script.GameInfo.Info.Stat;

namespace Script.Buff {
    public interface IBuffOwner {
        void AddStatus(List<StatusInfo> infos);
        void RemoveStatus(IEnumerable<StatusInfo> infos);
        // 버프 Speed 보너스의 fade 진행도 통보 (fadeFactor 0=시작, 1=최대)
        void OnBuffSpeedFade(float totalSpdBonus, float fadeFactor);
    }
}