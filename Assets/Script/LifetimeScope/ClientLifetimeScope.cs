using Script.Client;
using VContainer;
using VContainer.Unity;

// 여기서 Firebase 관련 로직 및 서버가 있다면 서버와 연결로직
// 터치 시 Addressable 최신화 및 GameInfo 초기화 등 작업이 진행
// 이 후 GroupLifetimeScope에서 Player의 세부 정보를 담는 Service들을 생성.
namespace Script.LifetimeScope {
    public class ClientLifetimeScope : VContainer.Unity.LifetimeScope {
        protected override void Configure(IContainerBuilder builder) {
            name = nameof(ClientLifetimeScope);

            builder.RegisterEntryPoint<GameClient>(Lifetime.Singleton)
                   .As<IClient>();
        }
    }
}