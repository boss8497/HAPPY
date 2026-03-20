using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using MessagePack;
using Script.DataBase.Interface;
using UnityEngine;

namespace Script.DataBase {
    public class FileStorage : IFileStorage {
        private const int BufferSize = 4096;

        public async UniTask<string> LoadJsonAsync(string path) {
            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"파일을 찾을 수 없습니다. Path: {fullPath}");

            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                useAsync: true
            );

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync().AsUniTask();
        }

        public async UniTask SaveJsonAsync(string path, string content) {
            var fullPath  = GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var bytes = Encoding.UTF8.GetBytes(content);

            await using var stream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                useAsync: true
            );

            await stream.WriteAsync(bytes, 0, bytes.Length).AsUniTask();
            await stream.FlushAsync().AsUniTask();
        }

        public async UniTask SaveMessagePackAsync<T>(string path, T value, MessagePackSerializerOptions options = null) {
            options ??= MessagePackSerializerOptions.Standard;
            var bytes = MessagePackSerializer.Serialize(value, options);
            await WriteAllBytesAsync(path, bytes);
        }

        public async UniTask<T> LoadMessagePackAsync<T>(string path, MessagePackSerializerOptions options = null) {
            options ??= MessagePackSerializerOptions.Standard;
            var bytes = await ReadAllBytesAsync(path);
            return MessagePackSerializer.Deserialize<T>(bytes, options);
        }
        
        private async UniTask<byte[]> ReadAllBytesAsync(string path) {
            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"파일을 찾을 수 없습니다. Path: {fullPath}");

            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                useAsync: true
            );

            var length = stream.Length;
            if (length > int.MaxValue)
                throw new IOException($"파일 크기가 너무 큽니다. Path: {fullPath}");

            var buffer = new byte[length];
            var offset = 0;

            while (offset < buffer.Length) {
                var read = await stream.ReadAsync(buffer, offset, buffer.Length - offset).AsUniTask();
                if (read == 0)
                    break;

                offset += read;
            }

            if (offset != buffer.Length)
                throw new EndOfStreamException($"파일을 끝까지 읽지 못했습니다. Path: {fullPath}");

            return buffer;
        }

        private async UniTask WriteAllBytesAsync(string path, byte[] data) {
            var fullPath  = GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await using var stream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                useAsync: true
            );

            await stream.WriteAsync(data, 0, data.Length).AsUniTask();
            await stream.FlushAsync().AsUniTask();
        }

        public bool Exists(string path) {
            return File.Exists(GetFullPath(path));
        }

        public UniTask DeleteAsync(string path) {
            var fullPath = GetFullPath(path);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return UniTask.CompletedTask;
        }

        public UniTask CopyAsync(string sourceRelativePath, string destinationRelativePath) {
            var sourcePath = GetFullPath(sourceRelativePath);
            var destPath   = GetFullPath(destinationRelativePath);

            var directory = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.Copy(sourcePath, destPath, overwrite: true);
            return UniTask.CompletedTask;
        }

        public string GetFullPath(string path) {
            return Path.Combine(Application.persistentDataPath, path);
        }
    }
}