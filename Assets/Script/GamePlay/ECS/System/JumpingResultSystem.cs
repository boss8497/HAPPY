using Script.GameInfo.Character;
using Script.GamePlay.ECS.Component;
using Unity.Entities;
using Unity.Transforms;

namespace Script.GamePlay.ECS.System {
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(JumpingSystem))]
    public partial class JumpingResultSystem : SystemBase {
        protected override void OnUpdate() {
            foreach (var (unitData, jumpResult)
                     in SystemAPI.Query<
                                     RefRO<UnitData>,
                                     RefRW<JumpResultData>>()
                                 .WithAll<UnitEntityTag>()) {
                var landed = jumpResult.ValueRO.Landed;
                if (landed <= 0) continue;
                
                var character = unitData.ValueRO.GameObject.Value.GetComponent<Character.Character>();
                character.RemoveState(CharacterState.Jumping);
                jumpResult.ValueRW.Landed = 0;
            }
        }
    }
}