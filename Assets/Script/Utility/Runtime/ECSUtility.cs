using System;
using Unity.Collections;

namespace Script.Utility.Runtime {
    public static class ECSUtility {
        public static int FindIndex<T>(this NativeList<T> list, T target)
            where T : unmanaged, System.IEquatable<T> {
            var firstIndex = 0;
            var lastIndex  = list.Length - 1;

            while (firstIndex <= lastIndex) {
                if (list[firstIndex].Equals(target))
                    return firstIndex;

                if (firstIndex != lastIndex && list[lastIndex].Equals(target))
                    return lastIndex;

                firstIndex++;
                lastIndex--;
            }

            return -1;
        }

        /// <summary>
        /// 순서를 유지하며 제거한다.
        /// </summary>
        public static bool RemoveValue<T>(this NativeList<T> list, T target)
            where T : unmanaged, System.IEquatable<T> {
            var index = list.FindIndex(target);

            if (index < 0)
                return false;

            list.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 순서를 유지하지 않고 빠르게 제거한다.
        /// 마지막 요소를 제거 위치로 옮긴다.
        /// </summary>
        public static bool RemoveValueSwapBack<T>(this NativeList<T> list, T target)
            where T : unmanaged, System.IEquatable<T> {
            var index = list.FindIndex(target);

            if (index < 0)
                return false;

            list.RemoveAtSwapBack(index);
            return true;
        }
        
        
        public static int FindIndex<T>(this NativeList<T> list, Predicate<T> predicate)
            where T : unmanaged, System.IEquatable<T> {
            var firstIndex = 0;
            var lastIndex  = list.Length - 1;

            while (firstIndex <= lastIndex) {
                if (predicate.Invoke(list[firstIndex]))
                    return firstIndex;

                if (firstIndex != lastIndex && predicate.Invoke(list[lastIndex]))
                    return lastIndex;

                firstIndex++;
                lastIndex--;
            }

            return -1;
        }
        
        
        /// <summary>
        /// 순서를 유지하며 제거한다.
        /// </summary>
        public static bool RemoveValue<T>(this NativeList<T> list, Predicate<T> predicate)
            where T : unmanaged, System.IEquatable<T> {
            var index = list.FindIndex(predicate);

            if (index < 0)
                return false;

            list.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 순서를 유지하지 않고 빠르게 제거한다.
        /// 마지막 요소를 제거 위치로 옮긴다.
        /// </summary>
        public static bool RemoveValueSwapBack<T>(this NativeList<T> list, Predicate<T> predicate)
            where T : unmanaged, System.IEquatable<T> {
            var index = list.FindIndex(predicate);

            if (index < 0)
                return false;

            list.RemoveAtSwapBack(index);
            return true;
        }
    }
}