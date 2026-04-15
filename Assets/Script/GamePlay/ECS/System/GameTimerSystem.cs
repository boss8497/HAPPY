using Script.GamePlay.ECS.Component;
using Unity.Burst;
using Unity.Entities;

namespace Script.GamePlay.ECS.System {
    
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    public partial struct GameTimerSystem : ISystem {
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