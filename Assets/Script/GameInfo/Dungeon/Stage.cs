using System;
using Script.GameInfo.Attribute;
using UnityEngine.SceneManagement;

namespace Script.GameInfo.Dungeon {
    [AutoEditorTable(true)]
    [Serializable]
    public class Stage : IEquatable<Stage> {
        public SerializeGuid guid = SerializeGuid.NewGuid();
        public string        GuidString => guid.ToString();

        public string id;

        [LocalizePath("@\"Stage/Name/\" + GuidString", false)]
        public string name;

        [LocalizePath("@\"Stage/Description/\" + GuidString", false)]
        public string description;

        [Phase]
        public int[] phaseInfos;


        [AssetPath(typeof(Scene))]
        public string scenePath = string.Empty;


        public static bool operator ==(Stage left, Stage right) {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left.guid == right.guid;
        }

        public static bool operator !=(Stage left, Stage right) {
            return !(left == right);
        }

        public bool Equals(Stage other) {
            return other is not null && guid == other.guid;
        }

        public override bool Equals(object obj) {
            return obj is Stage other && Equals(other);
        }

        public override int GetHashCode() {
            return guid.GetHashCode();
        }
    }
}