using VContainer;

namespace Script.LifetimeScope {
    public class TitleLifetimeScope : VContainer.Unity.LifetimeScope {
        protected override void Configure(IContainerBuilder builder) {
            name = nameof(TitleLifetimeScope);
            
        }
    }
}