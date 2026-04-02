using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct RunningSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<RunningData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            // FixedStepSimulationSystemGroup은 업데이트 동안 Time.DeltaTime을 고정 스텝 값으로 override한다.
            // 그래서 fixed-step 안에서는 DeltaTime이 “그 프레임에 실제 사용된 fixed step”이 된다.
            var fixedTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, running) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<RunningData>>()) {
                float3 dir = running.ValueRO.Direction;

                // 혹시 0벡터가 들어오면 이동 안 함
                if (math.lengthsq(dir) <= 0.000001f)
                    continue;

                transform.ValueRW.Position += dir * running.ValueRO.Speed * fixedTime;
            }
        }
    }
}