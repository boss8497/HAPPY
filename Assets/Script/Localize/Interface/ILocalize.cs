using UnityEngine.Localization;

namespace Script.Localize {
    public interface ILocalize {
        bool   IsInitialized { get; }
        Locale Locale        { get; }
    }
}