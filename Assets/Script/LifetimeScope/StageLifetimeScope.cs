using Script.GamePlay.Camera;
using Script.GamePlay.ECS.World;
using Script.GamePlay.Input;
using Script.GamePlay.Pool;
using Script.GamePlay.Stage;
using Script.GamePlay.Unit;
using Script.GamePlay.Unit.Interface;
using Script.LifetimeScope.Locator;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Script.LifetimeScope {
    public class StageLifetimeScope : VContainer.Unity.LifetimeScope {
        [SerializeField]
        private Camera mainCamera;

        [SerializeField]
        private CinemachineTargetGroup targetGroup;

        protected override void Configure(IContainerBuilder builder) {
            name = nameof(StageLifetimeScope);

            var locator = Parent.Container.Resolve<IScopeLocator>();
            locator.SetScope(ScopeType.Stage, this);

            builder.RegisterEntryPoint<StageEntityWorld>(Lifetime.Singleton)
                   .As<IStageEntityWorld>()
                   .AsSelf();

            builder.RegisterEntryPoint<StageManager>(Lifetime.Singleton)
                   .As<IStageManager>()
                   .WithParameter(targetGroup);
            builder.RegisterEntryPoint<StagePooling>(Lifetime.Singleton)
                   .As<IStagePooling>();

            builder.Register<ICameraControls, CameraControls>(Lifetime.Singleton)
                   .WithParameter<Camera>(mainCamera == null ? Camera.main : mainCamera);

            // 유저 조작 감지 Class이기 때문에 StageScope에 있는게 맞다.
            // 안의 PlayerControlMap을 Parent Scope를 로드하는게 좋아 보임
            builder.Register<IPlayerControls, PlayerControls>(Lifetime.Singleton);
            builder.Register<IUnitManager, UnitManager>(Lifetime.Singleton);
        }

        //TODO: 일단 이렇게 Locator에 지우고 있는데.. 음 조금 더 좋은 방법을 생각해봐야될듯? StageManager 자체가 GameObject에 있기 때문에 조금 애매하네
        protected override void OnDestroy() {
            base.OnDestroy();
            if (Parent?.Container != null) {
                var locator = Parent.Container.Resolve<IScopeLocator>();
                locator.ReleaseChildScope(ScopeType.Stage);
            }
        }
    }
}