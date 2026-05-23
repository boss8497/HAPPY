using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Enum;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using VContainer.Unity;

namespace Script.Localize {
    public class Localize : ILocalize, IInitializable {
        private readonly char _separator = '/';
        
        private Dictionary<ErrorMessage, string> _errorMessageDictionary;

        public bool   IsInitialized { get; private set; }
        public Locale Locale        { get; private set; }

        public void Initialize() {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync() {
            var initOperation = LocalizationSettings.InitializationOperation;
            if (!initOperation.IsDone) {
                await UniTask.WaitUntil(() => initOperation.IsDone
                );
            }

            SelectDefaultLocale();

            _errorMessageDictionary = Enum.GetNames(typeof(ErrorMessage))
                                       .ToDictionary(Enum.Parse<ErrorMessage>,  r => r);

            IsInitialized = true;
        }

        /// <summary>
        /// 현재는 일단 기본 한국어 나중에는 System 언어 확인해서 설정
        /// </summary>
        private void SelectDefaultLocale() {
            Locale = LocalizationSettings.AvailableLocales.GetLocale("ko");
        }

        private void DecomposeLocalizeKey(string term, out string tableName, out string entryName) {
            if (string.IsNullOrWhiteSpace(term))
                throw new ArgumentException("term is null or empty.", nameof(term));

            var index = term.IndexOf(_separator);

            if (index <= 0 || index >= term.Length - 1) {
                throw new ArgumentException(
                    "term is not valid. term must be {CollectionName}/{EntryName} format. - " + term,
                    nameof(term)
                );
            }

            tableName = term[..index];
            entryName = term[(index + 1)..];
        }

        public async UniTask<string> GetLocalizeText(
            string            term,
            params object[]   arguments
        ) {
            DecomposeLocalizeKey(term, out var tableName, out var entryName);

            return await LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                       tableName,
                       entryName,
                       Locale,
                       FallbackBehavior.UseProjectSettings,
                       arguments
                   );
        }

        public async UniTask<string> GetLocalizeText(
            string            tableName,
            string            entryName,
            params object[]   arguments
        ) {
            return await LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                       tableName,
                       entryName,
                       Locale,
                       FallbackBehavior.UseProjectSettings,
                       arguments
                   );
        }

        private string GetErrorMessageName(ErrorMessage errorMessage) {
            if (_errorMessageDictionary.TryGetValue(errorMessage, out var name)) {
                return name;
            }

            var messageName = errorMessage.ToString();
            _errorMessageDictionary.Add(errorMessage, errorMessage.ToString());
            return messageName;
        }
        
        public async UniTask<string> GetErrorMessage(ErrorMessage errorMessage) {
            return await GetLocalizeText(nameof(ErrorMessage), GetErrorMessageName(errorMessage));
        }
    }
}