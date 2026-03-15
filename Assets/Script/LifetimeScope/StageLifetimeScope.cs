using Script.GamePlay.Input;
using VContainer;

namespace Script.LifetimeScope {
    public class StageLifetimeScope : VContainer.Unity.LifetimeScope {
        protected override void Configure(IContainerBuilder builder) {
            
            //일단 테스트를 위해 Stage Scope에 등록
            //상위 Scope가 생기면 옮겨야 됨
            builder.Register<IPlayerControls, PlayerControls>(Lifetime.Singleton);
        }
    }
}