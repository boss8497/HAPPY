using Script.GameInfo.Info.Stat;

namespace Script.Buff {
    public interface IBuff {
        public long  Uid         { get; set; }
        StatusInfo[] StatusInfos { get; }
    }
}