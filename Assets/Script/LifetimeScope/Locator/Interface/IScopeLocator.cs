namespace Script.LifetimeScope.Locator {
    public interface IScopeLocator {
        bool Initialized { get; }
        void                           SetScope(ScopeType type, VContainer.Unity.LifetimeScope scope);
        VContainer.Unity.LifetimeScope GetScope(ScopeType type);
        VContainer.Unity.LifetimeScope GetRootScope();
    }
}