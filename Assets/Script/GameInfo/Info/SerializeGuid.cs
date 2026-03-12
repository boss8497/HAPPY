using System;
using UnityEngine;

namespace Script.GameInfo.Info {
    [Serializable]
    public sealed class SerializeGuid : ISerializationCallbackReceiver, IEquatable<SerializeGuid> {
        [SerializeField] private uint v0;
        [SerializeField] private uint v1;
        [SerializeField] private uint v2;
        [SerializeField] private uint v3;

        [NonSerialized] private Guid _value;
        [NonSerialized] private bool _cacheValid;

        public Guid Value {
            get {
                if (_cacheValid == false) {
                    _value      = ReadGuidFromFields();
                    _cacheValid = true;
                }

                return _value;
            }
            set {
                _value      = value;
                _cacheValid = true;
                WriteGuidToFields(value);
            }
        }
        public bool IsEmpty => Value == Guid.Empty;

        public SerializeGuid() {
            _value      = Guid.Empty;
            _cacheValid = true;
            WriteGuidToFields(_value);
        }

        public SerializeGuid(Guid value) {
            _value      = value;
            _cacheValid = true;
            v0         = 0;
            v1         = 0;
            v2         = 0;
            v3         = 0;
            WriteGuidToFields(value);
        }

        public static SerializeGuid NewGuid() {
            return new (Guid.NewGuid());
        }

        public void SetNewGuid() {
            Value = Guid.NewGuid();
        }

        public void Clear() {
            Value = Guid.Empty;
        }

        public override string ToString() {
            return Value.ToString("D");
        }

        public bool Equals(SerializeGuid other) {
            if (ReferenceEquals(other, null))
                return false;

            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj) {
            return obj is SerializeGuid other && Equals(other);
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        public static implicit operator Guid(SerializeGuid value) {
            return value?.Value ?? Guid.Empty;
        }

        public static implicit operator SerializeGuid(Guid value) {
            return new (value);
        }

        public void OnBeforeSerialize() {
            // 현재 캐시 값을 직렬화 필드로 반영
            if (_cacheValid)
                WriteGuidToFields(_value);
        }

        public void OnAfterDeserialize() {
            // 역직렬화
            _value      = ReadGuidFromFields();
            _cacheValid = true;
        }

        private void WriteGuidToFields(Guid guid) {
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);

            v0 = ToUInt32(bytes.Slice(0, 4));
            v1 = ToUInt32(bytes.Slice(4, 4));
            v2 = ToUInt32(bytes.Slice(8, 4));
            v3 = ToUInt32(bytes.Slice(12, 4));
        }

        private Guid ReadGuidFromFields() {
            Span<byte> bytes = stackalloc byte[16];

            WriteUInt32(bytes.Slice(0, 4), v0);
            WriteUInt32(bytes.Slice(4, 4), v1);
            WriteUInt32(bytes.Slice(8, 4), v2);
            WriteUInt32(bytes.Slice(12, 4), v3);

            return new Guid(bytes);
        }

        private static uint ToUInt32(ReadOnlySpan<byte> bytes) {
            return (uint)(
                             bytes[0]
                           | (bytes[1] << 8)
                           | (bytes[2] << 16)
                           | (bytes[3] << 24)
                         );
        }

        private static void WriteUInt32(Span<byte> bytes, uint value) {
            bytes[0] = (byte)(value);
            bytes[1] = (byte)(value >> 8);
            bytes[2] = (byte)(value >> 16);
            bytes[3] = (byte)(value >> 24);
        }
    }
}