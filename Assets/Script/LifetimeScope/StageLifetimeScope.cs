using Script.GamePlay.Camera;
using Script.GamePlay.Input;
using Script.GamePlay.Stage;
using Script.GamePlay.Unit;
using Script.GamePlay.Unit.Interface;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

namespace Script.LifetimeScope {
    public class StageLifetimeScope : VContainer.Unity.LifetimeScope {
        [SerializeField]
        private Camera mainCamera;
        
        [SerializeField]
        private CinemachineTargetGroup  targetGroup;

        protected override void Configure(IContainerBuilder builder) {
            name = nameof(StageLifetimeScope);
            
            builder.Register<IStageManager, StageManager>(Lifetime.Singleton)
                   .WithParameter(targetGroup);
            builder.Register<ICameraControls, CameraControls>(Lifetime.Singleton)
                   .WithParameter<Camera>(mainCamera == null ? Camera.main : mainCamera);

            // 유저 조작 감지 Class이기 때문에 StageScope에 있는게 맞다.
            // 안의 PlayerControlMap을 Parent Scope를 로드하는게 좋아 보임
            builder.Register<IPlayerControls, PlayerControls>(Lifetime.Singleton);
            builder.Register<IUnitManager, UnitManager>(Lifetime.Singleton);
        }
    }
}