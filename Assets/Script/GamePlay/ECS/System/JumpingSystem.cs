using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(RunningSystem))]
    public partial struct JumpingSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<JumpingData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var dt = SystemAPI.Time.DeltaTime;

            var groundY = SystemAPI.HasSingleton<MapGroundData>()
                ? SystemAPI.GetSingleton<MapGroundData>().GroundY
                : 0f;

            state.Dependency = new JumpingUpdateJob {
                Dt      = dt,
                GroundY = groundY,
            }.ScheduleParallel(state.Dependency);
        }

        [WithDisabled(typeof(UnitSystemControlEnable))]
        [BurstCompile]
        public partial struct JumpingUpdateJob : IJobEntity {
            public float Dt;
            public float GroundY;

            private void Execute(
                ref LocalTransform              transform,
                ref JumpingData                 jumping,
                ref JumpInputData               input,
                in  UnitData                    unit,
                EnabledRefRW<UnitJumpingEnable> jumpingEnable
            ) {
                // 플레이어는 항상 현재 맵 GroundY를 착지 기준으로 사용
                if (unit.IsPlayer != 0)
                    jumping.GroundY = GroundY;

                if (input.Held != 0 && jumping.CurrentJumpTime < jumping.MaxJumpTime) {
                    jumping.CurrentJumpTime += Dt;

                    if (jumping.CurrentJumpTime > jumping.MaxJumpTime) {
                        jumping.CurrentJumpTime = jumping.MaxJumpTime;
                    }
                }

                if (input.ReleaseRequested != 0) {
                    jumping.CurrentJumpTime = math.min(jumping.CurrentJumpTime, jumping.Timer);
                    jumping.CurrentJumpTime = math.max(jumping.CurrentJumpTime, jumping.MinJumpTime);
                    input.ReleaseRequested = 0;
                }

                if (jumping.Timer < jumping.CurrentJumpTime && jumping.JumpVelocity > 0f) {
                    jumping.JumpVelocity -= jumping.Gravity * Dt * 0.5f;
                }
                else {
                    jumping.JumpVelocity -= jumping.Gravity * jumping.FallGravity * Dt;
                }

                var position = transform.Position;
                position.y += jumping.JumpVelocity * Dt;

                if (position.y <= jumping.GroundY && jumping.Timer > 0f) {
                    position.y = jumping.GroundY;
                    transform.Position = position;

                    input.Held             = 0;
                    input.ReleaseRequested = 0;
                    jumpingEnable.ValueRW  = false;
                    return;
                }

                transform.Position = position;
                jumping.Timer += Dt;
            }
        }
    }
}
