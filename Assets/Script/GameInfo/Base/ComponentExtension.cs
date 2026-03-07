using Script.Utility.Runtime;

namespace Script.GameInfo.Base {
    public static class ComponentExtension {
        public static T GetComponent<T>(this InfoBase infoBase) where T : class, IComponent {
            foreach (var component in infoBase.Components) {
                if (component is T tComponent) {
                    return tComponent;
                }
            }
            return null;
        }
        
        public static T[] GetComponents<T>(this InfoBase infoBase) where T : class, IComponent {
            var poolList = ListPool.Get<T>();
            foreach (var component in infoBase.Components) {
                if (component is T tComponent) {
                    poolList.Add(tComponent);
                }
            }

            var result = poolList.ToArray();
            ListPool.Return(poolList);
            return result;
        }
        
        public static bool TryGetComponent<T>(this InfoBase infoBase, out T result) where T : class, IComponent {
            foreach (var component in infoBase.Components) {
                if (component is T tComponent) {
                    result = tComponent;
                    return true;
                }
            }
            result = null;
            return false;
        }
        
        
        public static T GetComponent<T>(this IComponent[] components) where T : class, IComponent {
            foreach (var component in components) {
                if (component is T tComponent) {
                    return tComponent;
                }
            }
            return null;
        }
        
        public static bool TryGetComponent<T>(this IComponent[] components, out T result) where T : class, IComponent {
            foreach (var component in components) {
                if (component is T tComponent) {
                    result = tComponent;
                    return true;
                }
            }
            result = null;
            return false;
        }
        
        public static T[] GetComponents<T>(this IComponent[] components) where T : class, IComponent {
            var poolList = ListPool.Get<T>();
            foreach (var component in components) {
                if (component is T tComponent) {
                    poolList.Add(tComponent);
                }
            }
            
            var result = poolList.ToArray();
            ListPool.Return(poolList);
            return result;
        }
    }
}