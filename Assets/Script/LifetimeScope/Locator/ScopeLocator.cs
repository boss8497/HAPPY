using System.Collections.Generic;
using VContainer.Unity;

namespace Script.LifetimeScope.Locator {
    public class ScopeLocator : IScopeLocator, IInitializable {
        private readonly Dictionary<ScopeType, VContainer.Unity.LifetimeScope> _scopes = new();
        private readonly VContainer.Unity.LifetimeScope _root;
        
        

        public bool Initialized { get; private set; }

        public ScopeLocator(VContainer.Unity.LifetimeScope root) {
            //처음에 Set되는거는 Root
            _root = root;
        }

        public void Initialize() {
            SetScope(ScopeType.App, _root);
            Initialized = true;
        }

        public void SetScope(ScopeType type, VContainer.Unity.LifetimeScope scope) {
            if (_scopes.TryGetValue(type, out VContainer.Unity.LifetimeScope existingScope)) {
                existingScope?.Dispose();
                _scopes[type] = null;
            }
            _scopes[type] = scope;
        }

        public VContainer.Unity.LifetimeScope GetScope(ScopeType type) {
            return _scopes.GetValueOrDefault(type);
        }
        
        public VContainer.Unity.LifetimeScope GetRootScope() {
            return _scopes.GetValueOrDefault(ScopeType.App);
        }
    }
}