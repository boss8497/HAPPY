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
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            _query = SystemAPI.QueryBuilder()
                              .WithAllRW<LocalTransform>()
                              .WithAll<RunningData, UnitRunningEnable>()
                              .WithDisabled<UnitDieEnable, UnitSystemControlEnable>()
                              .Build();

            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var fixedTime = SystemAPI.Time.DeltaTime;

            state.Dependency = new RunningJob {
                DeltaTime = fixedTime
            }.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        private partial struct RunningJob : IJobEntity {
            public float DeltaTime;

            private void Execute(ref LocalTransform transform, in RunningData running) {
                var dir = running.Direction;

                if (math.lengthsq(dir) <= 0.000001f)
                    return;

                transform.Position += dir * running.Speed * DeltaTime;
            }
        }
    }
}