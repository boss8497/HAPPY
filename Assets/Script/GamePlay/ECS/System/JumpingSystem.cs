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
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (transformRef, jumpingRef, inputRef, resultRef, entity) in
                     SystemAPI.Query<
                             RefRW<LocalTransform>,
                             RefRW<JumpingData>,
                             RefRW<JumpInputData>,
                             RefRW<JumpResultData>>()
                         .WithEntityAccess()) {
                ref var transform = ref transformRef.ValueRW;
                ref var jumping   = ref jumpingRef.ValueRW;
                ref var input     = ref inputRef.ValueRW;
                ref var result    = ref resultRef.ValueRW;

                result.Landed = 0;

                // 점프 버튼을 계속 누르고 있으면 상승 유지 시간 증가
                if (input.Held != 0 && jumping.CurrentJumpTime < jumping.MaxJumpTime) {
                    jumping.CurrentJumpTime += dt;
                    if (jumping.CurrentJumpTime > jumping.MaxJumpTime) {
                        jumping.CurrentJumpTime = jumping.MaxJumpTime;
                    }
                }

                // 버튼을 뗀 순간은 1회성 이벤트처럼 소비
                if (input.ReleaseRequested != 0) {
                    jumping.CurrentJumpTime = math.min(jumping.CurrentJumpTime, jumping.Timer);
                    input.ReleaseRequested = 0;
                }

                // 상승 / 하강 구간 중력 적용
                if (jumping.Timer < jumping.CurrentJumpTime && jumping.JumpVelocity > 0f) {
                    jumping.JumpVelocity -= jumping.Gravity * dt * 0.5f;
                }
                else {
                    jumping.JumpVelocity -= jumping.Gravity * jumping.FallGravity * dt;
                }

                var position = transform.Position;
                position.y += jumping.JumpVelocity * dt;

                // 바닥 도달
                if (position.y <= jumping.GroundY && jumping.Timer > 0f) {
                    position.y = jumping.GroundY;
                    transform.Position = position;

                    result.Landed = 1;
                    input.Held = 0;
                    input.ReleaseRequested = 0;

                    state.EntityManager.SetComponentEnabled<JumpingData>(entity, false);
                    continue;
                }

                transform.Position = position;
                jumping.Timer += dt;
            }
        }
    }
}