using Script.GamePlay.Pool;
using Script.LifetimeScope.Locator;
using Script.Localize;
using VContainer;

namespace Script.GUI.Screen {
    public partial class ScreenManager {
        private IScopeLocator _scopeLocator;
        private ILocalize     _localize;
        private IUIPooling    _uiPooling;

        [Inject]
        public void Constructor(
            IScopeLocator scopeLocator,
            ILocalize     localize,
            IUIPooling    uiPooling
        ) {
            _scopeLocator = scopeLocator;
            _localize     = localize;
            _uiPooling    = uiPooling;
        }
    }
}