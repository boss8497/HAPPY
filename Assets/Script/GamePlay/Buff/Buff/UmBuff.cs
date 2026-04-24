using System;

namespace Script.Buff {
    public struct UmBuff : IEquatable<UmBuff> {
        public float endTime;
        public long  buffUid;

        public bool Equals(UmBuff other) {
            return endTime.Equals(other.endTime)
                && buffUid == other.buffUid;
        }

        public override bool Equals(object obj) {
            return obj is UmBuff other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(endTime, buffUid);
        }
    }
}