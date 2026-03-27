using Script.DataBase;
using Script.DataBase.Interface;
using Script.GameInfo.Attribute;
using Script.GameSetting.Interface;
using Script.GUI.Screen;
using Script.GUI.Screen.Interface;
using Script.LifetimeScope.Interface;
using Script.LifetimeScope.Locator;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Script.LifetimeScope {
    // Root LifetimeScope
    // 일단 포함한 부분은 ScreenManager
    // 경험에 의하면 Title에서도 UI를 사용해야되는데
    // 참여한 모든 프로젝트가 Title -> Game 입장 시 중간단계에 ScreenManager가 생성 되어 매우 불편했음.
    // 굳이 StartUp.scene을 사용해서 한 단계를 거치는 이유는
    // GameInitialize를 위해 설정
    // 이 후 ClientLifetimeScope 생성
    
    public class AppLifetimeScope : VContainer.Unity.LifetimeScope {
        [AssetPath(typeof(GameObject))]
        public string screenManagerPath;
        
        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterEntryPoint<GameSetting.GameSetting>(Lifetime.Singleton)
                   .As<IGameSetting>();
            
            
            builder.RegisterEntryPoint<ScopeLocator>(Lifetime.Singleton)
                   .As<IScopeLocator>();
            builder.Register<IScopeFactory, ScopeFactory>(Lifetime.Singleton);
            
            builder.Register<IFileStorage, FileStorage>(Lifetime.Singleton);
            builder.Register<IDataBase, GameDataBase>(Lifetime.Singleton);
            
            RegisterScreenManager(builder);
        }

        private void RegisterScreenManager(IContainerBuilder builder) {
            //Load ScreenManager
            var activeScene = SceneManager.GetActiveScene();
            var handle      = Addressables.LoadAssetAsync<GameObject>(screenManagerPath);
            handle.WaitForCompletion();
            var asset = handle.Result;
            
            var screenManagerInstance = Object.Instantiate(asset);
            var screenManager         = screenManagerInstance.GetComponent<ScreenManager>();

            builder.RegisterComponent(screenManager)
                   .As<IScreenManager>()
                   .AsSelf();
            
            Addressables.Release(handle);
        }
    }
}