using System;
using System.Collections.Generic;

namespace Script.Utility.Runtime {
    public interface IClassPool {
        void OnRent();   // Get 직후
        void OnReturn(); // Release 직전 (정리)
    }

    public static class ClassPool {
        private static readonly Dictionary<Type, Stack<object>> Pools = new(64);

        public static T Get<T>() where T : class, new() {
            var type = typeof(T);

            if (Pools.TryGetValue(type, out var stack) && stack.Count > 0) {
                var obj = (T)stack.Pop();
                if (obj is IClassPool p) p.OnRent();
                return obj;
            }

            var created = new T();
            if (created is IClassPool cp) cp.OnRent();
            return created;
        }
        
        public static T Get<T>(Func<T> factory) where T : class {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var type = typeof(T);

            if (Pools.TryGetValue(type, out var stack) && stack.Count > 0) {
                var obj = (T)stack.Pop();
                if (obj is IClassPool p) p.OnRent();
                return obj;
            }

            var created = factory();
            if (created == null) throw new InvalidOperationException("factory returned null.");
            if (created is IClassPool cp) cp.OnRent();
            return created;
        }

        public static void Release<T>(T obj) where T : class {
            if (obj == null) return;

            if (obj is IClassPool p) p.OnReturn();

            var type = typeof(T);
            if (!Pools.TryGetValue(type, out var stack)) {
                stack       = new Stack<object>(32);
                Pools[type] = stack;
            }

            stack.Push(obj);
        }

        public static void Clear<T>() where T : class {
            Pools.Remove(typeof(T));
        }

        public static void ClearAll() {
            Pools.Clear();
        }
    }
}