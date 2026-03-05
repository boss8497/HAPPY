using System;
using System.Collections.Generic;

namespace Expression {
    public static class ValueStringKey {
        // Ordinal 권장 (빠르고 문화권 영향 없음)
        private static readonly Dictionary<string, int> StringKeyCache = new(StringComparer.Ordinal);

        private static          int    _nextId;
        private static readonly object _lock = new();

        // 기존 API 유지
        public static int GetKey(string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return GetKey(name.AsSpan());
        }

        // 새 API: ReadOnlySpan<char>
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
                // 폴백: span -> string 변환 (조회도 할당 1회)
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