using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Script.Utility.Runtime {
    public static class ArrayUtility {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Exists<T>(this T[] array, Predicate<T> pred) {
            if (array == null) return false;
            if (pred == null) return false;

            foreach (var t in array) {
                if (pred(t))
                    return true;
            }

            return false;
        }

        public static T[] FindAll<T>(this T[] array, Predicate<T> pred) {
            if (array == null) return Array.Empty<T>();
            if (pred == null) return Array.Empty<T>();

            int count = 0;
            foreach (var t in array) {
                if (pred(t))
                    count++;
            }

            if (count == 0)
                return Array.Empty<T>();

            var result = new T[count];
            int dst    = 0;

            foreach (var t in array) {
                if (pred(t))
                    result[dst++] = t;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Find<T>(this T[] array, Predicate<T> pred) {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (pred == null) throw new ArgumentNullException(nameof(pred));

            foreach (var item in array) {
                if (pred(item))
                    return item;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindIndex<T>(this T[] array, Predicate<T> pred) {
            if (array == null) return -1;
            if (pred == null) return -1;

            for (int i = 0; i < array.Length; i++) {
                if (pred(array[i]))
                    return i;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this T[] array, T value) {
            if (array == null) throw new ArgumentNullException(nameof(array));

            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < array.Length; i++) {
                if (comparer.Equals(array[i], value))
                    return i;
            }

            return -1;
        }
    }
}