using UnityEngine;

namespace Script.GameInfo.Base {
    public static class ComponentExtension {
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
            var resultList = new System.Collections.Generic.List<T>();
            foreach (var component in components) {
                if (component is T tComponent) {
                    resultList.Add(tComponent);
                }
            }
            return resultList.ToArray();
        }
    }
}