using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Utility.Runtime {
    public static class ListPool {
        private static readonly Dictionary<Type, Stack<ICollection>> Pools = new Dictionary<Type, Stack<ICollection>>(64);

        public static List<T> Get<T>() {
            var type = typeof(T);

            if (!Pools.TryGetValue(type, out var stack)) {
                stack       = new Stack<ICollection>(32);
                Pools[type] = stack;
            }

            if (stack.Count > 0) {
                var list = (List<T>)stack.Pop();
                if (list == null) {
                    list = new List<T>();
                    Debug.LogError($"[ListPool] 리스트 풀링 에러 로직 확인!!");
                }
                return list;
            }
            return new();
        }

        public static void Return<T>(List<T> list) {
            if (list == null) return;
            list.Clear();

            var type = typeof(T);
            if (!Pools.TryGetValue(type, out var stack)) {
                stack       = new Stack<ICollection>(32);
                Pools[type] = stack;
            }

            stack.Push(list);
        }
        
        
        // GetCollection은 Return 시 Clear를 호출해줘야 됨.
        public static T GetCollection<T>() where T : class, ICollection, new() {
            var type = typeof(T);

            if (!Pools.TryGetValue(type, out var stack)) {
                stack       = new Stack<ICollection>(32);
                Pools[type] = stack;
            }

            if (stack.Count > 0) {
                var list = (T)stack.Pop();
                if (list == null) {
                    list = new T();
                    Debug.LogError($"[ListPool] 리스트 풀링 에러 로직 확인!!");
                }
                return list;
            }
            return new ();
        }
        
        public static void ReturnCollection(ICollection list) {
            if (list == null) return;

            var type = list.GetType();
            if (!Pools.TryGetValue(type, out var stack)) {
                stack       = new Stack<ICollection>(32);
                Pools[type] = stack;
            }

            stack.Push(list);
        }
    }
}