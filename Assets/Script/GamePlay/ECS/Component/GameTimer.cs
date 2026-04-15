using Unity.Entities;

namespace Script.GamePlay.ECS.Component {
    public struct GameTimer : IComponentData {
        public float Elapsed; // 게임이 실제로 진행된 시간
        public float Delta;   // 이번 fixed step에서 증가한 시간
        public bool  IsPaused;
    }
}