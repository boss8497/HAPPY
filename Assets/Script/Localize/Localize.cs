using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

namespace Script.Localize {
    public class Localize : ILocalize, IInitializable {
        private readonly char _separator = '/';
        
        public bool   IsInitialized { get; private set; }
        public Locale Locale        { get; private set;}

        public void Initialize() {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync() {
            var initOperation = LocalizationSettings.InitializationOperation;
            if (!initOperation.IsDone) {
                await UniTask.WaitUntil(
                    () => initOperation.IsDone
                );
            }
            SelectDefaultLocale();
            
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
        
        private AsyncOperationHandle<string> GetLocalizeTextHandle(
            string          term,
            params object[] arguments
        ) {
            DecomposeLocalizeKey(term, out var tableName, out var entryName);

            return LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                tableName,
                entryName,
                Locale,
                FallbackBehavior.UseProjectSettings,
                arguments
            );
        }

        public UniTask<string> GetLocalizeTextAsync(
            string          term,
            params object[] arguments
        ) {
            return GetLocalizeTextAsync(term, CancellationToken.None, arguments);
        }

        public async UniTask<string> GetLocalizeTextAsync(
            string            term,
            CancellationToken cancellationToken,
            params object[]   arguments
        ) {
            var handle = GetLocalizeTextHandle(term, arguments);

            if (!handle.IsDone) {
                await UniTask.WaitUntil(
                    () => handle.IsDone,
                    cancellationToken: cancellationToken
                );
            }

            return handle.Status == AsyncOperationStatus.Succeeded
                       ? handle.Result
                       : null;
        }
        
        public string GetLocalizeText(
            string          term,
            params object[] arguments
        ) {
            var initOperation = LocalizationSettings.InitializationOperation;

            if (!initOperation.IsDone)
                initOperation.WaitForCompletion();

            var handle = GetLocalizeTextHandle(term, Locale, arguments);

            if (!handle.IsDone)
                handle.WaitForCompletion();

            return handle.Status == AsyncOperationStatus.Succeeded
                       ? handle.Result
                       : null;
        }
    }
}