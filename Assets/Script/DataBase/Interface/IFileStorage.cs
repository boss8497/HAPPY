using Cysharp.Threading.Tasks;
using MessagePack;

namespace Script.DataBase.Interface {
    public interface IFileStorage {
        bool Exists(string path);

        UniTask<string> LoadJsonAsync(string  path);
        UniTask         SaveJsonAsync(string path, string content);

        UniTask    SaveMessagePackAsync<T>(string path, T                            value,        MessagePackSerializerOptions options = null);
        UniTask<T> LoadMessagePackAsync<T>(string path, MessagePackSerializerOptions options = null);

        UniTask DeleteAsync(string path);
        UniTask CopyAsync(string   sourceRelativePath, string destinationRelativePath);

        string GetFullPath(string path);
    }
}