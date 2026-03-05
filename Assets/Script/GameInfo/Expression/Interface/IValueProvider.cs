using System;

namespace Expression.Interface {
    public interface IValueProvider {
        public bool TryGetValue(int key, out double value);
    }
}