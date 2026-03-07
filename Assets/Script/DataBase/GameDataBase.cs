using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Script.DataBase.Interface;
using UnityEngine;

namespace Script.DataBase {
    public class GameDataBase : IDataBase {
        private readonly IFileStorage _fileStorage;

        private bool _isInitialized = false;
        public  bool IsInitialized => _isInitialized;


        public GameDataBase(
            IFileStorage fileStorage
        ) {
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        }

        public async UniTask InitializeAsync() {
            _isInitialized = true;
        }
        
        public async UniTask<T> LoadAsync<T>(string path) {
            if (!_isInitialized)
                throw new InvalidOperationException("GameDataBase is not initialized.");

            if (!_fileStorage.Exists(path))
                return default;

            var json = await _fileStorage.ReadAllTextAsync(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async UniTask SaveAsync(string path, string json) {
            if (!_isInitialized)
                throw new InvalidOperationException("GameDataBase is not initialized.");
            await _fileStorage.WriteAllTextAsync(path, json);
        }
    }
}