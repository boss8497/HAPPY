using System;
using System.Collections;
using System.Collections.Generic;

namespace Script.Utility.Runtime {
    public static class ListPool {
        private static readonly Dictionary<Type, Stack<IList>> Pools = new Dictionary<Type, Stack<IList>>(64);

        public static List<T> Get<T>() {
            var type = typeof(T);

            if (!Pools.TryGetValue(type, out var stack)) {
                stack       = new Stack<IList>(32);
                Pools[type] = stack;
            }

            if (stack.Count > 0) {
                var list = (List<T>)stack.Pop();
                return list;
            }
            return new();
        }

        public static void Return<T>(List<T> list) {
            if (list == null) return;
            list.Clear();

            var type = typeof(T);
            if (!Pools.TryGetValue(type, out var stack)) {
                stack       = new Stack<IList>(32);
                Pools[type] = stack;
            }

            stack.Push(list);
        }
    }
}