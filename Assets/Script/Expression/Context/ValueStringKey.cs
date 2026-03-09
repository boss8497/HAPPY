using System;
using System.Collections.Generic;

namespace Expression {
    public static class ValueStringKey {
        // Ordinal 권장 (빠르고 문화권 영향 없음)
        private static readonly Dictionary<string, int> StringKeyCache = new(StringComparer.Ordinal) {
            { "level", 0 },
            { "grade", 1 },
            { "tier", 2 },
        };

        private static          int    _nextId;
        private static readonly object _lock = new();

        public static string GetName(int value) {
            lock (_lock) {
                foreach (var kvp in StringKeyCache) {
                    if (kvp.Value == value)
                        return kvp.Key;
                }
            }
            throw new KeyNotFoundException($"Key {value} not found.");
        }
        
        public static int GetKey(string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return GetKey(name.AsSpan());
        }

        public static int GetKey(ReadOnlySpan<char> name) {
            if (name.Length == 0) throw new ArgumentException("Key name is empty.", nameof(name));

            lock (_lock) {
#if NET8_0_OR_GREATER
                // .NET 8+: string 키 Dictionary에서 span으로 할당 없이 조회 가능
                var lookup = StringKeyCache.GetAlternateLookup<ReadOnlySpan<char>>();

                if (lookup.TryGetValue(name, out int key))
                    return key;

                // 새 키 추가 시에는 string이 필요하므로 여기서만 할당
                string s = new string(name);
                key = _nextId++;
                StringKeyCache.Add(s, key);
                return key;
#else
                string s = name.ToString();
                if (StringKeyCache.TryGetValue(s, out int key))
                    return key;

                key = _nextId++;
                StringKeyCache.Add(s, key);
                return key;
#endif
            }
        }
    }
}