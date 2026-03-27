using System;
using Script.LifetimeScope.Locator;
using VContainer;

namespace Script.LifetimeScope.Interface {
    public interface IScopeFactory {
        VContainer.Unity.LifetimeScope CreateScope(ScopeType type);
        VContainer.Unity.LifetimeScope CreateScope(ScopeType type, Action<IContainerBuilder> installation);
    }
}