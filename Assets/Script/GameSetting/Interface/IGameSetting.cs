using Script.GameSetting.Data;

namespace Script.GameSetting.Interface {
    public interface IGameSetting {
        GameSettingData GameSettingData { get; }
        bool Initialized { get; }
        
        string StartUpScenePath { get; }
        string TitleScenePath   { get; }
    }
}