using System;
using System.Collections.Generic;
using System.Threading;
using Expression.Interface;

namespace Expression {
    public class ValueContext : IDisposable {
        private static readonly AsyncLocal<Stack<IValueProvider>> ValueStack = new ();
        
        public ValueContext(IValueProvider valueProvider) {
            ValueStack.Value ??= new(32);
            ValueStack.Value.Push(valueProvider);
        }
        
        public void Dispose() {
            ValueStack.Value.Pop();
        }

        public static bool TryGetValue(string name, out double value) {
            value = 0;
            var key = ValueStringKey.GetKey(name);
            if (ValueStack.Value != null) {
                foreach (var provider in ValueStack.Value) {
                    if (provider.TryGetValue(key, out value)) {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public static bool TryGetValue(int key, out double value) {
            value = 0;
            if (ValueStack.Value != null) {
                foreach (var provider in ValueStack.Value) {
                    if (provider.TryGetValue(key, out value)) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}