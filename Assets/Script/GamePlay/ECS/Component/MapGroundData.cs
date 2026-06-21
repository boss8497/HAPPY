using Unity.Entities;

namespace Script.GamePlay.ECS.Component {
    // Stage 전역 바닥 Y를 담는 Singleton 컴포넌트.
    // StageManager.Map.cs에서 생성·갱신, RunningSystem에서 읽는다.
    public struct MapGroundData : IComponentData {
        public float GroundY;
    }
}
