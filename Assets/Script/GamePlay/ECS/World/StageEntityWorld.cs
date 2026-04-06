using System;
using Script.GamePlay.ECS.System;
using Unity.Collections;
using Unity.Entities;
using VContainer.Unity;

namespace Script.GamePlay.ECS.World {
    public class StageEntityWorld : IStageEntityWorld, IInitializable, IDisposable {
        private Unity.Entities.World _world;
        private bool                 _appendedToPlayerLoop;

        public Unity.Entities.World World => _world;

        public EntityManager EntityManager {
            get {
                if (IsAlive == false)
                    throw new InvalidOperationException($"{nameof(StageEntityWorld)} is not alive.");

                return _world.EntityManager;
            }
        }

        public bool IsAlive => _world is { IsCreated: true } && _appendedToPlayerLoop;

        public void Initialize() {
            if (IsAlive)
                return;

            _world = new Unity.Entities.World(nameof(StageEntityWorld), WorldFlags.Game);

            // 루트 그룹 먼저 생성
            _world.GetOrCreateSystemManaged<InitializationSystemGroup>();
            _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            _world.GetOrCreateSystemManaged<PresentationSystemGroup>();

            var systems = new NativeList<SystemTypeIndex>(Allocator.Temp);

            systems.Add(TypeManager.GetSystemTypeIndex<UpdateWorldTimeSystem>());
            
            // Simulation 트리의 built-in sibling들
            systems.Add(TypeManager.GetSystemTypeIndex<BeginSimulationEntityCommandBufferSystem>());
            systems.Add(TypeManager.GetSystemTypeIndex<FixedStepSimulationSystemGroup>());
            systems.Add(TypeManager.GetSystemTypeIndex<LateSimulationSystemGroup>());
            systems.Add(TypeManager.GetSystemTypeIndex<EndSimulationEntityCommandBufferSystem>());

            // FixedStep 안에서 ECB 쓸 수 있게
            systems.Add(TypeManager.GetSystemTypeIndex<BeginFixedStepSimulationEntityCommandBufferSystem>());
            systems.Add(TypeManager.GetSystemTypeIndex<EndFixedStepSimulationEntityCommandBufferSystem>());

            // 네 시스템들
            systems.Add(TypeManager.GetSystemTypeIndex<RunningSystem>());
            systems.Add(TypeManager.GetSystemTypeIndex<JumpingSystem>());
            systems.Add(TypeManager.GetSystemTypeIndex<JumpingResultSystem>());
            systems.Add(TypeManager.GetSystemTypeIndex<CharacterCollisionSystem>());
            systems.Add(TypeManager.GetSystemTypeIndex<CharacterCollisionResultSystem>());
            systems.Add(TypeManager.GetSystemTypeIndex<CharacterSyncSystem>());

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(_world, systems);
            systems.Dispose();

            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(_world);
            _appendedToPlayerLoop = true;
        }

        public void Dispose() {
            if (_world == null)
                return;

            if (_world.IsCreated) {
                if (_appendedToPlayerLoop) {
                    ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(_world);
                    _appendedToPlayerLoop = false;
                }

                _world.Dispose();
            }

            _world = null;
        }
    }
}