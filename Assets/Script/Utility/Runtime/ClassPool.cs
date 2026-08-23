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

        // ※ ClassListPoolDebugWindow.cs(Tools/Debug/Class & List Pool)가 리플렉션으로 직접 참조함
        // (Editor/README.md 체크리스트 참고)
        private static readonly Dictionary<Type, HashSet<object>> Pools = new(64);

        public static T Get<T>() where T : class, new() {
            var type = typeof(T);

            if (Pools.TryGetValue(type, out var set)) {
                var obj = Take<T>(set);
                if (obj is IClassPool p) p.OnRent();
                return (T)obj;
            }
            
            set         = new HashSet<object>(ReferenceComparer.Instance);
            Pools[type] = set;

            var created = new T();
            if (created is IClassPool cp) cp.OnRent();
            return created;
        }

        public static void Release<T>(T obj) where T : class {
            if (obj == null) return;

            var type = obj.GetType();

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
        
        private static object Take<T>(HashSet<object> set) where T : class, new() {
            if (set.Count <= 0) {
                var created = new T();
                if (created is IClassPool cp) cp.OnRent();
                return created;
            }
            
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