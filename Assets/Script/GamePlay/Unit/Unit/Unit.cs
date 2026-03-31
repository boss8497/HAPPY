using UnityEngine;

namespace Script.GamePlay.Unit {
    [System.Serializable]
    public abstract class Unit : MonoBehaviour {
        private long _uid;
        private int  _team;

        public long UID  => _uid;
        public int  Team => _team;

        public abstract Vector2   Position  { get; }
        public abstract Transform Transform { get; }

        public void Set(long uid, int team) {
            _uid  = uid;
            _team = team;
        }
    }
}