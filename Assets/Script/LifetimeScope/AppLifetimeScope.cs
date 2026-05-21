using Script.DataBase;
using Script.DataBase.Interface;
using Script.GameInfo.Attribute;
using Script.GamePlay.Pool;
using Script.GamePlay.Scene;
using Script.GameSetting.Interface;
using Script.GUI.Screen;
using Script.GUI.Screen.Interface;
using Script.LifetimeScope.Interface;
using Script.LifetimeScope.Locator;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

        /// <summary>
        /// 순서대로 초기화가 진행되는건 아니지만
        /// 여기 순서대로 초기화 순서를 가져간다고 생각하자.
        /// IsInitialize 같은 대기를 위해서
        /// </summary>
        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterEntryPoint<Addressable.Addressable>(Lifetime.Singleton)
                   .As<Addressable.IAddressable>();
            
            builder.RegisterEntryPoint<GameSetting.GameSetting>(Lifetime.Singleton)
                   .As<IGameSetting>();
            
            builder.RegisterEntryPoint<ScopeLocator>(Lifetime.Singleton)
                   .As<IScopeLocator>();

            builder.RegisterEntryPoint<GameTimer.GameTimer>(Lifetime.Singleton)
                   .As<GameTimer.IGameTimer>();

            builder.Register<IScopeFactory, ScopeFactory>(Lifetime.Singleton);

            builder.Register<IFileStorage, FileStorage>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameDataBase>(Lifetime.Singleton)
                   .As<IDataBase>();
            
            
            builder.RegisterEntryPoint<SceneLoader>(Lifetime.Singleton)
                   .As<ISceneLoader>();
            
            builder.RegisterEntryPoint<UIPooling>(Lifetime.Singleton)
                   .As<IUIPooling>();

            RegisterScreenManager(builder);
            
            builder.RegisterEntryPoint<Localize.Localize>(Lifetime.Singleton)
                   .As<Localize.ILocalize>();
        }

        private void RegisterScreenManager(IContainerBuilder builder) {
            //Load ScreenManager
            var handle = Addressables.LoadAssetAsync<GameObject>(screenManagerPath);
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