using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    /// <summary>
    /// 점프 중이 아닌 플레이어가 GroundY 보다 FallDetectionThreshold 이상 높은 위치에 있으면
    /// UnitFallingEnable 을 활성화해 GravitySystem 이 낙하 물리를 처리하도록 한다.
    /// </summary>
    [DisableAutoCreation]
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(RunningSystem))]
    [UpdateBefore(typeof(JumpingSystem))]
    public partial struct FallDetectionSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<FallingData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (!SystemAPI.HasSingleton<MapGroundData>()) return;

            state.Dependency = new FallDetectionJob {
                GroundY = SystemAPI.GetSingleton<MapGroundData>().GroundY,
            }.ScheduleParallel(state.Dependency);
        }

        [WithDisabled(typeof(UnitJumpingEnable), typeof(UnitFallingEnable),
                      typeof(UnitDieEnable),    typeof(UnitSystemControlEnable))]
        [BurstCompile]
        public partial struct FallDetectionJob : IJobEntity {
            public float GroundY;

            private void Execute(
                in  LocalTransform              transform,
                ref FallingData                 falling,
                in  UnitData                    unit,
                EnabledRefRW<UnitFallingEnable> fallingEnable
            ) {
                if (unit.IsPlayer == 0) return;

                if (transform.Position.y > GroundY + falling.FallDetectionThreshold) {
                    falling.FallVelocity  = 0f;
                    fallingEnable.ValueRW = true;
                }
            }
        }
    }
}
