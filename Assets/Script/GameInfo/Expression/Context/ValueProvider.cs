using System.Collections.Generic;
using Expression.Interface;

namespace Expression {
    public class ValueProvider : IValueProvider {
        private Dictionary<int, double> _values = new();
        
        public ValueProvider Add(string name, double value) {
            var key = ValueStringKey.GetKey(name);
            _values[key] = value;
            return this;
        }

        public bool TryGetValue(string name, out double value) {
            var key = ValueStringKey.GetKey(name);
            return _values.TryGetValue(key, out value);
        }

        public bool TryGetValue(int key, out double value) {
            return _values.TryGetValue(key, out value);
        }
    }
}