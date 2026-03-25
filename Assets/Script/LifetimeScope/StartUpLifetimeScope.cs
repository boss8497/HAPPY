using Script.GUI;
using Script.GUI.Interface;
using Script.StartUp;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Script.LifetimeScope {
    public class StartUpLifetimeScope : VContainer.Unity.LifetimeScope {
        [SerializeField]
        private ScreenManager screenManager;
        
        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterComponent(screenManager)
                   .As<IScreenManager>();

            builder.RegisterEntryPoint<GameStartUp>(Lifetime.Scoped);
        }
    }
}