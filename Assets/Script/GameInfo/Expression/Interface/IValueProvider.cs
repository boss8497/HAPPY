using System;

namespace Expression.Interface {
    public interface IValueProvider {
        public bool TryGetValue(string name, out double value);
        public bool TryGetValue(int key, out double value);
    }
}