using Unity.Entities;

namespace Script.GamePlay.ECS.Component {
    // Stage 전역 바닥 Y와 낙사 라인을 담는 Singleton 컴포넌트.
    // StageManager.Map.cs에서 생성·갱신, RunningSystem / GravitySystem에서 읽는다.
    public struct MapGroundData : IComponentData {
        public float GroundY;
        // 낙사 판정 Y (MapSpawnAction.fallDeathY 에서 초기화)
        public float FallDeathY;
        // 1이면 FallDeathY 활성화 (MapSpawnAction이 없으면 0)
        public byte  FallDeathEnabled;
    }
}
