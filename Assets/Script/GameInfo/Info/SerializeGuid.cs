using System;
using UnityEngine;

namespace Script.GameInfo.Info {
    [Serializable]
    public struct SerializeGuid : ISerializationCallbackReceiver, IEquatable<SerializeGuid> {
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

        public SerializeGuid(Guid value) {
            v0         = 0;
            v1         = 0;
            v2         = 0;
            v3         = 0;
            _value      = value;
            _cacheValid = true;
            WriteGuidToFields(value);
        }

        public static SerializeGuid NewGuid() {
            return new SerializeGuid(Guid.NewGuid());
        }

        public static SerializeGuid Empty() {
            return new SerializeGuid(Guid.Empty);
        }

        public void OnBeforeSerialize() {
            if (_cacheValid)
                WriteGuidToFields(_value);
        }

        public void OnAfterDeserialize() {
            _value      = ReadGuidFromFields();
            _cacheValid = true;
        }

        public override string ToString() {
            return Value.ToString("D");
        }

        public bool Equals(SerializeGuid other) {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj) {
            return obj is SerializeGuid other && Equals(other);
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        public static implicit operator Guid(SerializeGuid value) => value.Value;
        public static implicit operator SerializeGuid(Guid value) => new SerializeGuid(value);

        private void WriteGuidToFields(Guid guid) {
            var bytes = guid.ToByteArray();

            v0 = ToUInt32(bytes, 0);
            v1 = ToUInt32(bytes, 4);
            v2 = ToUInt32(bytes, 8);
            v3 = ToUInt32(bytes, 12);
        }

        private Guid ReadGuidFromFields() {
            var bytes = new byte[16];

            WriteUInt32(bytes, 0, v0);
            WriteUInt32(bytes, 4, v1);
            WriteUInt32(bytes, 8, v2);
            WriteUInt32(bytes, 12, v3);

            return new Guid(bytes);
        }

        private static uint ToUInt32(byte[] bytes, int offset) {
            return (uint)(
                             bytes[offset]
                           | (bytes[offset + 1] << 8)
                           | (bytes[offset + 2] << 16)
                           | (bytes[offset + 3] << 24));
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value) {
            bytes[offset]     = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }
    }
}