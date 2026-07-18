using MessagePack;

namespace Script.GamePlay.Audio.Model {
    /// <summary>
    /// 그룹별 볼륨/뮤트 설정. 현재는 Json으로 저장하지만 GroupModel 선례를 따라
    /// 추후 MessagePack 전환을 대비해 Key 어트리뷰트를 붙여둔다.
    /// </summary>
    [MessagePackObject]
    public class AudioSettingModel {
        [Key(0)] public float masterVolume = 1f;
        [Key(1)] public float bgmVolume    = 0.7f;
        [Key(2)] public float effectVolume = 1f;
        [Key(3)] public float voiceVolume  = 1f;

        [Key(4)] public bool masterMute;
        [Key(5)] public bool bgmMute;
        [Key(6)] public bool effectMute;
        [Key(7)] public bool voiceMute;

        public static AudioSettingModel CreateDefault() => new();
    }
}
