using Script.GamePlay.Service;
using Script.GamePlay.Service.Interface;
using VContainer;
using VContainer.Unity;

namespace Script.LifetimeScope {
    public class GroupLifetimeScope : VContainer.Unity.LifetimeScope {
        protected override void Configure(IContainerBuilder builder) {
            name = nameof(GroupLifetimeScope);

            builder.RegisterEntryPoint<GroupService>(Lifetime.Singleton)
                   .As<IGroupService>();
        }
    }
}