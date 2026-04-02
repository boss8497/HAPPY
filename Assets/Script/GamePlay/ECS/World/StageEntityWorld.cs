using System;
using Script.GamePlay.ECS.System;
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

        public bool IsAlive => _world is { IsCreated: true };

        public void Initialize() {
            if (IsAlive)
                return;

            _world = new(nameof(StageEntityWorld), WorldFlags.Game);

            _world.GetOrCreateSystemManaged<InitializationSystemGroup>();
            _world.GetOrCreateSystemManaged<PresentationSystemGroup>();

            var simGroup        = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            // 나중에 추가될 SystemGroup이 있다면 여기에 추가
            // var lateGroup       = _world.GetOrCreateSystemManaged<LateSimulationSystemGroup>();
            // var fixedStepGroup = _world.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>();
            // simGroup.AddSystemToUpdateList(lateGroup);
            // simGroup.AddSystemToUpdateList(fixedStepGroup);

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(
                _world,
                typeof(CharacterCollisionSystem),
                typeof(RunningSystem),
                typeof(CharacterSyncSystem)
            );

            simGroup.SortSystems();

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