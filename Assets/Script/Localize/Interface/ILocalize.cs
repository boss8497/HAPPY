using Cysharp.Threading.Tasks;
using Script.GameInfo.Enum;
using UnityEngine.Localization;

namespace Script.Localize {
    public interface ILocalize {
        bool   IsInitialized { get; }
        Locale Locale        { get; }

        UniTask<string> GetLocalizeText(
            string          term,
            params object[] arguments
        );
        UniTask<string> GetLocalizeText(
            string          tableName,
            string          entryName,
            params object[] arguments
        );

        UniTask<string> GetErrorMessage(ErrorMessage errorMessage);
    }
}