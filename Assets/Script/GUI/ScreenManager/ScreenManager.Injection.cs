using Script.GamePlay.Pool;
using Script.LifetimeScope.Locator;
using VContainer;

namespace Script.GUI.Screen {
    public partial class ScreenManager {
        private IScopeLocator _scopeLocator;
        private IUIPooling    _uiPooling;

        [Inject]
        public void Constructor(
            IScopeLocator scopeLocator,
            IUIPooling    uiPooling
        ) {
            _scopeLocator = scopeLocator;
            _uiPooling    = uiPooling;
        }
    }
}