using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Script.Utility.Runtime {
    public interface IClassPool {
        void OnRent();   // Get 직후
        void OnReturn(); // Release 직전 (정리)
    }

    public static class ClassPool {
        private sealed class ReferenceComparer : IEqualityComparer<object> {
            public static readonly ReferenceComparer Instance = new();
            bool IEqualityComparer<object>.          Equals(object      x, object y) => ReferenceEquals(x, y);
            int IEqualityComparer<object>.           GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }

        private static readonly Dictionary<Type, HashSet<object>> Pools = new(64);

        public static T Get<T>() where T : class, new() {
            var type = typeof(T);

            if (Pools.TryGetValue(type, out var set) && set.Count > 0) {
                var obj = Take(set);
                if (obj is IClassPool p) p.OnRent();
                return (T)obj;
            }

            var created = new T();
            if (created is IClassPool cp) cp.OnRent();
            return created;
        }

        public static T Get<T>(Func<T> factory) where T : class {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var type = typeof(T);

            if (Pools.TryGetValue(type, out var set) && set.Count > 0) {
                var obj = Take(set);
                if (obj is IClassPool p) p.OnRent();
                return (T)obj;
            }

            var created = factory();
            if (created == null) throw new InvalidOperationException("factory returned null.");
            if (created is IClassPool cp) cp.OnRent();
            return created;
        }

        public static void Release<T>(T obj) where T : class {
            if (obj == null) return;

            var type = typeof(T);

            if (!Pools.TryGetValue(type, out var set)) {
                set         = new HashSet<object>(ReferenceComparer.Instance);
                Pools[type] = set;
            }

            if (!set.Add(obj)) {
                Debug.LogWarning($"[ClassPool] {type.Name} 인스턴스가 이미 반환된 상태에서 다시 Release 되었습니다. 중복 반환은 무시합니다.");
                return;
            }

            if (obj is IClassPool p) p.OnReturn();
        }
        
        private static object Take(HashSet<object> set) {
            using var e = set.GetEnumerator();
            e.MoveNext();
            var obj = e.Current;
            set.Remove(obj);
            return obj;
        }

        public static void Clear<T>() where T : class {
            Pools.Remove(typeof(T));
        }

        public static void ClearAll() {
            Pools.Clear();
        }
    }
}