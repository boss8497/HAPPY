using System;
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
            //미리 생성
            for (int i = 0; i < (int)ScopeType.Max; ++i) {
                var scopeType =  (ScopeType)i;
                _scopes.Add(scopeType, null);
            }
            
            SetScope(ScopeType.App, _root);
            Initialized = true;
        }

        public void SetScope(ScopeType type, VContainer.Unity.LifetimeScope scope) {
            if (scope == null) return;
            
            ReleaseChildScope(type);
            _scopes[type] = scope;
        }

        public VContainer.Unity.LifetimeScope GetScope(ScopeType type) {
            return _scopes.GetValueOrDefault(type);
        }
        
        public VContainer.Unity.LifetimeScope GetRootScope() {
            return _scopes.GetValueOrDefault(ScopeType.App);
        }
        
        public VContainer.Unity.LifetimeScope GetLastChildScope() {
            for (int i = ((int)ScopeType.Max - 1); i >= 0; --i) {
                
                var scopeType =  (ScopeType)i;
                if (_scopes[scopeType] != null) {
                    return _scopes[scopeType];
                }
            }
            return null;
        }

        public VContainer.Unity.LifetimeScope GetParentScope(ScopeType type) {
            if (type == ScopeType.App || type == ScopeType.Max) {
                throw new ArgumentException($"Invalid scope type: {type}");
            }
            var index = (int)type - 1;
            return _scopes[(ScopeType)index];
        }

        // 하위 Scope 자동 Dispose
        // Parent Scope가 Dispose되면 VContainer에서 자동 Dispose 해주지만 
        // 명시적으로 표시
        public void ReleaseChildScope(ScopeType type) {
            var index = (int)type;
            for (int i = (int)ScopeType.Max - 1; i >= index; --i) {
                var scopeType =  (ScopeType)i;

                //이미 생성 되어 있다는 가정
                _scopes[scopeType]?.Dispose();
                _scopes[scopeType] = null;
            }
        }
    }
}