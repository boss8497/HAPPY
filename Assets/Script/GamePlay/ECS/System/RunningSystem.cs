using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RunningSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<RunningData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var fixedTime = SystemAPI.Time.fixedDeltaTime;

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