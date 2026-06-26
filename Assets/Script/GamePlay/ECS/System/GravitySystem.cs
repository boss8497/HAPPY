using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(JumpingSystem))]
    public partial struct GravitySystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<FallingData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (!SystemAPI.HasSingleton<MapGroundData>()) return;

            state.Dependency = new GravityJob {
                Dt      = SystemAPI.Time.DeltaTime,
                GroundY = SystemAPI.GetSingleton<MapGroundData>().GroundY,
            }.ScheduleParallel(state.Dependency);
        }

        [WithDisabled(typeof(UnitSystemControlEnable))]
        [BurstCompile]
        public partial struct GravityJob : IJobEntity {
            public float Dt;
            public float GroundY;

            private void Execute(
                ref LocalTransform              transform,
                ref FallingData                 falling,
                in  UnitData                    unit,
                EnabledRefRW<UnitFallingEnable> fallingEnable
            ) {
                var groundY = unit.IsPlayer != 0 ? GroundY : 0f;

                falling.FallVelocity -= falling.Gravity * falling.FallGravity * Dt;
                var pos = transform.Position;
                pos.y += falling.FallVelocity * Dt;

                if (pos.y <= groundY) {
                    pos.y                 = groundY;
                    falling.FallVelocity  = 0f;
                    fallingEnable.ValueRW = false;
                }

                transform.Position = pos;
            }
        }
    }
}
