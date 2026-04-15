using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Entities;

namespace Script.GamePlay.ECS.System {
    
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    public partial struct GameTimerSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            if (!SystemAPI.TryGetSingleton<GameTimer>(out _)) {
                state.EntityManager.CreateSingleton(new GameTimer {
                    Elapsed  = 0f,
                    Delta    = 0f,
                    IsPaused = false
                });
            }
            state.RequireForUpdate<GameTimer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var gameTime = SystemAPI.GetSingletonRW<GameTimer>();

            if (gameTime.ValueRO.IsPaused) {
                gameTime.ValueRW.Delta = 0f;
                return;
            }

            var dt = SystemAPI.Time.DeltaTime;
            gameTime.ValueRW.Delta   =  dt;
            gameTime.ValueRW.Elapsed += dt;
        }
    }
}