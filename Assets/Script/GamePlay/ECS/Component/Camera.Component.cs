using Script.GamePlay.Camera;
using Unity.Entities;

namespace Script.GamePlay.ECS.Component {
    public struct CameraEntityTag : IComponentData { }

    public struct CameraData : IComponentData {
        public Entity                             Entity;
        public UnityObjectRef<UnityEngine.Camera> Camera;
    }
}