using Script.DataBase;
using Script.DataBase.Interface;
using Script.GamePlay.Camera;
using Script.GamePlay.Input;
using Script.GamePlay.Service;
using Script.GamePlay.Service.Interface;
using Script.GamePlay.Stage;
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
            builder.Register<IFileStorage, FileStorage>(Lifetime.Singleton);
            builder.Register<IDataBase, GameDataBase>(Lifetime.Singleton);
            builder.Register<IGroupService, GroupService>(Lifetime.Singleton);
            builder.Register<IStageManager, StageManager>(Lifetime.Singleton)
                   .WithParameter(targetGroup);

            

            builder.Register<ICameraControls, CameraControls>(Lifetime.Singleton)
                   .WithParameter<Camera>(mainCamera == null ? Camera.main : mainCamera);

            
            //일단 테스트를 위해 Stage Scope에 등록
            //상위 Scope가 생기면 옮겨야 됨
            builder.Register<IPlayerControls, PlayerControls>(Lifetime.Singleton);
        }
    }
}