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
            state.RequireForUpdate<MapGroundData>();
            
            _query = SystemAPI.QueryBuilder()
                              .WithAllRW<LocalTransform>()
                              .WithAll<RunningData, UnitData, FallingData, UnitRunningEnable>()
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

            private void Execute(
                ref LocalTransform transform,
                in  RunningData    running,
                in  UnitData       unit,
                in  FallingData    falling
            ) {
                // 낙사 구간(IsLethalFall=1)에서는 플레이어 X 이동 차단
                if (unit.IsPlayer != 0 && falling.IsLethalFall != 0) return;

                var dir = running.Direction;
                if (math.lengthsq(dir) <= 0.000001f) return;
                transform.Position += dir * running.Speed * DeltaTime;
            }
        }

        // GroundY 변경 시 지면 위에 있는 플레이어를 위로 스냅(오르막 경사).
        // 내리막 경사는 FallDetectionSystem + GravitySystem이 자연스럽게 처리.
        [WithDisabled(typeof(UnitJumpingEnable))]
        [WithDisabled(typeof(UnitFallingEnable))]
        [WithDisabled(typeof(UnitDieEnable))]
        [BurstCompile]
        private partial struct SnapPlayerToGroundJob : IJobEntity {
            public float GroundY;

            private void Execute(ref LocalTransform transform, in UnitData unit) {
                if (unit.IsPlayer == 0) return;
                var pos = transform.Position;
                if (GroundY >= pos.y) {
                    pos.y              = GroundY;
                    transform.Position = pos;
                }
            }
        }
    }
}
