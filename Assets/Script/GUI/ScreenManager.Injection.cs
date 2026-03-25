using VContainer;

namespace Script.GUI {
    public partial class ScreenManager {
        private IObjectResolver _resolver;

        [Inject]
        public void Constructor(
            IObjectResolver resolver
        ) {
            _resolver       = resolver;
        }
    }
}