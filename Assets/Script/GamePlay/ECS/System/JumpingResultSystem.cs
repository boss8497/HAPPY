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
            foreach (var unitData
                     in SystemAPI.Query<RefRO<UnitData>>()
                                 .WithDisabled<UnitJumpingEnable, UnitSystemControlEnable>()
                                 .WithAll<UnitEntityTag>()) {
                var character = unitData.ValueRO.GameObject.Value.GetComponent<Character.Character>();
                if ((character.Jumping?.CurrentValue ?? false) == false) continue;
                character.RemoveState(CharacterState.Jumping);
            }
        }
    }
}