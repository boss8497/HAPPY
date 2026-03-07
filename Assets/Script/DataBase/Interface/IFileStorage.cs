using Cysharp.Threading.Tasks;

/// <summary>
/// 파일 저장소 인터페이스
/// </summary>
namespace Script.DataBase.Interface {
    public interface IFileStorage {
        public bool            Exists(string            path);
        public UniTask<string> ReadAllTextAsync(string  path);
        public UniTask         WriteAllTextAsync(string path, string content);
        public UniTask         DeleteAsync(string       path);
        public UniTask         CopyAsync(string         sourceRelativePath, string destinationRelativePath);
        public string          GetFullPath(string       path);
    }
}