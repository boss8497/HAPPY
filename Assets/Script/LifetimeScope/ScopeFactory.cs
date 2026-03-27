using System;
using JetBrains.Annotations;
using Script.LifetimeScope.Interface;
using Script.LifetimeScope.Locator;
using VContainer;

namespace Script.LifetimeScope {
    public class ScopeFactory : IScopeFactory {
        private readonly IScopeLocator _locator;

        public ScopeFactory(
            IScopeLocator locator
        ) {
            _locator = locator;
        }

        // ScopeType.App과 ScopeType.Max는 요청할 수 없다
        public VContainer.Unity.LifetimeScope CreateScope(ScopeType type) {
            return CreateScope(type, null);
        }

        public VContainer.Unity.LifetimeScope CreateScope(ScopeType type, Action<IContainerBuilder> installation) {
            if (type == ScopeType.App || type == ScopeType.Max) {
                throw new ArgumentException($"Invalid scope type: {type}");
            }

            var parent = _locator.GetParentScope(type);
            if (parent == null) {
                throw new Exception($"Parent scope type {type} is not supported");
            }

            var scope =  installation == null ? CreateScope(parent, type) : CreateScope(parent, type, installation);
            _locator.SetScope(type, scope);
            return scope;
        }

        private VContainer.Unity.LifetimeScope CreateScope(VContainer.Unity.LifetimeScope parent, ScopeType type, Action<IContainerBuilder> installation = null)
            => (type) switch {
                ScopeType.Client => parent.CreateChild<ClientLifetimeScope>(installation),
                ScopeType.Group  => parent.CreateChild<GroupLifetimeScope>(installation),
                ScopeType.Stage  => parent.CreateChild<StageLifetimeScope>(installation),
                _                => null
            };
        
        private VContainer.Unity.LifetimeScope CreateScope(VContainer.Unity.LifetimeScope parent, ScopeType type)
            => (type) switch {
                ScopeType.Client => parent.CreateChild<ClientLifetimeScope>(),
                ScopeType.Group  => parent.CreateChild<GroupLifetimeScope>(),
                ScopeType.Stage  => parent.CreateChild<StageLifetimeScope>(),
                _                => null
            };
    }
}