using System.Collections.Generic;

namespace Expression {
    public static class ValueStringKey {
        private static readonly Dictionary<string, int> StringKeyCache = new();
        
        public static int GetKey(string name) {
            if (StringKeyCache.TryGetValue(name, out var key)) {
                return key;
            }
            StringKeyCache[name] = StringKeyCache.Count;
            return key;
        }
    }
}