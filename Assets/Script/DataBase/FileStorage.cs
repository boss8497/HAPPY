using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Script.DataBase.Interface;
using UnityEngine;

namespace Script.DataBase {
    public class FileStorage : IFileStorage {
        public bool Exists(string path) {
            return File.Exists(GetFullPath(path));
        }

        public async UniTask<string> ReadAllTextAsync(string path) {
            var fullPath = GetFullPath(path);

            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true
            );

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync().AsUniTask();
        }

        public async UniTask WriteAllTextAsync(string path, string content) {
            var fullPath  = GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var bytes = Encoding.UTF8.GetBytes(content);

            using var stream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true
            );

            await stream.WriteAsync(bytes, 0, bytes.Length).AsUniTask();
            await stream.FlushAsync().AsUniTask();
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