using UnityEngine;

namespace Script.GamePlay.Unit {
    [System.Serializable]
    public abstract class Unit : MonoBehaviour {
        protected long _uid;
        protected int  _team;

        public long UID  => _uid;
        public int  Team => _team;

        public abstract Vector2   Position  { get; }
        public abstract Transform Transform { get; }
        public abstract bool      IsPlayer  { get; }

        public void Set(long uid, int team) {
            _uid  = uid;
            _team = team;
        }
    }
}