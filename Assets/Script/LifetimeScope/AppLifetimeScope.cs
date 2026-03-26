using Script.GameInfo.Attribute;
using Script.GUI;
using Script.GUI.Interface;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Script.LifetimeScope {
    public class AppLifetimeScope : VContainer.Unity.LifetimeScope {
        [AssetPath(typeof(GameObject))]
        public string screenManagerPath;
        
        protected override void Configure(IContainerBuilder builder) {
            Initialize(builder);
        }

        private void Initialize(IContainerBuilder builder) {
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