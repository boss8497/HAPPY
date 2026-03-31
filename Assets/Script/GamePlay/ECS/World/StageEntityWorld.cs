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
            _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            _world.GetOrCreateSystemManaged<PresentationSystemGroup>();

            // 나중에 ECS 판정 시스템을 붙일 거면 player loop에 연결해두는 게 편함.
            // 지금은 시스템이 없어도 붙여놔도 문제는 없음.
            
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(
                _world,
                typeof(CharacterCollisionSystem)
            );

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