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
        private float       _lastGroundY;
        private bool        _groundYValid;

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

            if (!SystemAPI.HasSingleton<MapGroundData>()) {
                _groundYValid = false;
                return;
            }

            var groundY = SystemAPI.GetSingleton<MapGroundData>().GroundY;

            // GroundY가 바뀐 경우에만 플레이어 스냅 처리
            if (_groundYValid && math.abs(_lastGroundY - groundY) < 0.0001f) return;

            _lastGroundY  = groundY;
            _groundYValid = true;

            state.Dependency = new SnapPlayerToGroundJob {
                GroundY = groundY
            }.ScheduleParallel(state.Dependency);
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

        // GroundY 변경 시 점프 중이 아닌 플레이어만 Y 즉시 스냅
        [WithDisabled(typeof(UnitJumpingEnable))]
        [WithDisabled(typeof(UnitDieEnable))]
        [BurstCompile]
        private partial struct SnapPlayerToGroundJob : IJobEntity {
            public float GroundY;

            private void Execute(ref LocalTransform transform, in UnitData unit) {
                if (unit.IsPlayer == 0) return;
                var pos = transform.Position;
                pos.y              = GroundY;
                transform.Position = pos;
            }
        }
    }
}
