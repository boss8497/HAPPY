using Unity.Entities;

namespace Script.GamePlay.ECS.World {
    public interface IStageEntityWorld {
        Unity.Entities.World World         { get; }
        EntityManager        EntityManager { get; }
        bool                 IsAlive       { get; }
    }
}