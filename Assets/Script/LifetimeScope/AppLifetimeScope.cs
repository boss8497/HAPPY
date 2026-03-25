using Script.GUI;
using Script.GUI.Interface;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Script.LifetimeScope {
    public class AppLifetimeScope : VContainer.Unity.LifetimeScope {
        [SerializeField]
        private ScreenManager screenManager;
        
        protected override void Configure(IContainerBuilder builder) {
            DontDestroyOnLoad(this);
            name = nameof(AppLifetimeScope);
            
            builder.RegisterComponent(screenManager)
                   .As<IScreenManager>();
        }
    }
}