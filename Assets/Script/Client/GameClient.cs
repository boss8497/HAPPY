using Script.LifetimeScope.Locator;
using VContainer.Unity;

namespace Script.Client {
    // 각 종 플러그인 (Firebase, Steam, AD) 등 필요한 플러그인을 초기화 하는 Class
    // 필요 플러그인 Class를 만들고 Inject을 받아
    // 여기서 초기화!
    // 아직은 없음.
    public class GameClient : IClient, IInitializable {
        private readonly IScopeLocator _scopeLocator;


        public GameClient(
            IScopeLocator scopeLocator
        ) {
            _scopeLocator = scopeLocator;
        }


        public void Initialize() {
        }
    }
}