using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Script.Localize.Text {
    [Serializable]
    public class LocalizeText {
        [SerializeField]
        private LocalizedString _localizedString = new LocalizedString();

        public LocalizedString LocalizedString => _localizedString;

        public bool IsEmpty => _localizedString == null || _localizedString.IsEmpty;

        public string TableName {
            get {
                if (_localizedString == null)
                    return string.Empty;

                return _localizedString.TableReference.TableCollectionName;
            }
        }

        public string Key {
            get {
                if (_localizedString == null)
                    return string.Empty;

                return _localizedString.TableEntryReference.Key;
            }
        }

        public LocalizeText() { }

        public LocalizeText(string tableName, string key) {
            SetReference(tableName, key);
        }

        public void SetReference(string tableName, string key) {
            if (_localizedString == null)
                _localizedString = new LocalizedString();

            _localizedString.SetReference(tableName, key);
        }

        public string GetText() {
            if (IsEmpty)
                return string.Empty;

            return _localizedString.GetLocalizedString();
        }

        public string GetText(params object[] arguments) {
            if (IsEmpty)
                return string.Empty;

            return _localizedString.GetLocalizedString(arguments);
        }

        public AsyncOperationHandle<string> GetTextAsync() {
            if (_localizedString == null)
                _localizedString = new LocalizedString();

            return _localizedString.GetLocalizedStringAsync();
        }

        public void Register(LocalizedString.ChangeHandler onChanged) {
            if (onChanged == null)
                return;

            if (_localizedString == null)
                _localizedString = new LocalizedString();

            _localizedString.StringChanged += onChanged;
        }

        public void Unregister(LocalizedString.ChangeHandler onChanged) {
            if (onChanged == null || _localizedString == null)
                return;

            _localizedString.StringChanged -= onChanged;
        }

        public void Refresh() {
            if (_localizedString == null)
                return;

            _localizedString.RefreshString();
        }

        public override string ToString() {
            return $"{TableName}/{Key}";
        }
    }
}