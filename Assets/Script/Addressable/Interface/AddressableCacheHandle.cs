using System;

namespace Script.Addressable {
    public sealed class AddressableCacheHandle<T> : IDisposable where T : UnityEngine.Object {
        private readonly Action _release;
        private          bool   _disposed;

        public T Value { get; }

        public AddressableCacheHandle(T value, Action release) {
            Value    = value;
            _release = release;
        }

        public void Dispose() {
            if (_disposed) return;
            _disposed = true;
            _release?.Invoke();
        }
    }
}
