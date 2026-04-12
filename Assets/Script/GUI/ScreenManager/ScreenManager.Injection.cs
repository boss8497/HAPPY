using Script.LifetimeScope.Locator;
using VContainer;

namespace Script.GUI.Screen {
    public partial class ScreenManager {
        private IScopeLocator _scopeLocator;

        [Inject]
        public void Constructor(
            IScopeLocator scopeLocator
        ) {
            _scopeLocator = scopeLocator;
        }
    }
}