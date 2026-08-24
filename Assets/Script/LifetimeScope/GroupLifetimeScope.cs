using Script.GamePlay.Service;
using Script.GamePlay.Service.Interface;
using Script.GamePlay.Stage;
using VContainer;
using VContainer.Unity;

namespace Script.LifetimeScope {
    public class GroupLifetimeScope : VContainer.Unity.LifetimeScope {
        protected override void Configure(IContainerBuilder builder) {
            name = nameof(GroupLifetimeScope);

            builder.RegisterEntryPoint<GroupService>(Lifetime.Singleton)
                   .As<IGroupService>();
            
            builder.RegisterEntryPoint<ItemService>(Lifetime.Singleton)
                   .As<IItemService>();
            
            builder.RegisterEntryPoint<TutorialService>(Lifetime.Singleton)
                   .As<ITutorialService>();
            
            builder.RegisterEntryPoint<FocusService>(Lifetime.Singleton)
                   .As<IFocusService>();
            
            builder.RegisterEntryPoint<NarrationService>(Lifetime.Singleton)
                   .As<INarrationService>();
            
        }
    }
}