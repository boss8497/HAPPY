using System;
using System.IO;
using Cysharp.Threading.Tasks;
using MessagePack;
using Newtonsoft.Json;
using Script.DataBase.Enum;
using Script.DataBase.Interface;
using VContainer.Unity;

namespace Script.DataBase {
    public partial class GameDataBase : IDataBase, IInitializable, IDisposable {
        private static readonly MessagePackSerializerOptions MessagePackOptions =
            MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private readonly IFileStorage _fileStorage;

        public bool Initialized { get; private set; }

        public GameDataBase(IFileStorage fileStorage) {
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        }

        public void Initialize() {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync() {
            await InitializeItemTable();
            Initialized = true;
        }
        
        public async UniTask<T> LoadAsync<T>(string path, DataType type = DataType.Json) {
            switch (type) {
                case DataType.Json:
                    return await LoadJsonAsync<T>(path);
                case DataType.MessagePack:
                    return await LoadMessagePackAsync<T>(path);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public UniTask SaveAsync<T>(string path, T data, DataType type = DataType.Json) {
            switch (type) {
                case DataType.Json:
                    return SaveJsonAsync(path, data);
                case DataType.MessagePack:
                    return SaveMessagePackAsync(path, data);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public bool Exists(string path) {
            return _fileStorage.Exists(path);
        }

        private async UniTask<T> LoadJsonAsync<T>(string path) {
            EnsureInitialized();

            if (!_fileStorage.Exists(path))
                return default;

            var json = await _fileStorage.LoadJsonAsync(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private async UniTask SaveJsonAsync<T>(string path, T data, Formatting formatting = Formatting.None) {
            EnsureInitialized();

            var json = JsonConvert.SerializeObject(data, formatting);
            await _fileStorage.SaveJsonAsync(path, json);
        }

        private async UniTask<T> LoadMessagePackAsync<T>(string path) {
            EnsureInitialized();

            if (!_fileStorage.Exists(path))
                return default;

            return await _fileStorage.LoadMessagePackAsync<T>(path, MessagePackOptions);
        }

        private async UniTask SaveMessagePackAsync<T>(string path, T data) {
            EnsureInitialized();
            await _fileStorage.SaveMessagePackAsync(path, data, MessagePackOptions);
        }

        private void EnsureInitialized() {
            if (!Initialized)
                throw new InvalidOperationException("GameDataBase is not initialized.");
        }

        public void Dispose() {
            // TODO release managed resources here
        }
    }
}