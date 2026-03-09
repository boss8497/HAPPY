using System;
using System.Collections.Generic;
using Expression.Interface;

namespace Expression {
    public class ValueProvider : IValueProvider {
        private Dictionary<int, double> _values = new();
        
        public ValueProvider Add(ReadOnlySpan<char> name, double value) {
            var key = ValueStringKey.GetKey(name);
            _values[key] = value;
            return this;
        }

        public bool TryGetValue(int key, out double value) {
            return _values.TryGetValue(key, out value);
        }
    }
}