using Script.GamePlay.Camera;
using Script.GamePlay.Input;
using UnityEngine;
using VContainer;

namespace Script.LifetimeScope {
    public class StageLifetimeScope : VContainer.Unity.LifetimeScope {
        [SerializeField]
        private Camera mainCamera; 
        
        protected override void Configure(IContainerBuilder builder) {
            
            builder.Register<ICameraControls, CameraControls>(Lifetime.Singleton)
                   .WithParameter<Camera>(mainCamera == null ? Camera.main : mainCamera);
            
            //일단 테스트를 위해 Stage Scope에 등록
            //상위 Scope가 생기면 옮겨야 됨
            builder.Register<IPlayerControls, PlayerControls>(Lifetime.Singleton);
        }
    }
}