using System;
using MessagePack;

namespace Script.GamePlay.Audio.Model {
    /// <summary>
    /// 그룹별 볼륨/뮤트 설정. 현재는 Json으로 저장하지만 GroupModel 선례를 따라
    /// 추후 MessagePack 전환을 대비해 Key 어트리뷰트를 붙여둔다.
    /// </summary>
    [MessagePackObject]
    public class AudioSettingModel : ICloneable, IEquatable<AudioSettingModel> {
        [Key(0)] public float masterVolume = 1f;
        [Key(1)] public float bgmVolume    = 0.7f;
        [Key(2)] public float effectVolume = 1f;
        [Key(3)] public float voiceVolume  = 1f;

        [Key(4)] public bool masterMute;
        [Key(5)] public bool bgmMute;
        [Key(6)] public bool effectMute;
        [Key(7)] public bool voiceMute;

        public static AudioSettingModel CreateDefault() => new();

        public object Clone() {
            return new AudioSettingModel {
                masterVolume = masterVolume,
                bgmVolume    = bgmVolume,
                effectVolume = effectVolume,
                voiceVolume  = voiceVolume,
                masterMute   = masterMute,
                bgmMute      = bgmMute,
                effectMute   = effectMute,
                voiceMute    = voiceMute,
            };
        }

        public bool Equals(AudioSettingModel other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return masterVolume.Equals(other.masterVolume)
                && bgmVolume.Equals(other.bgmVolume)
                && effectVolume.Equals(other.effectVolume)
                && voiceVolume.Equals(other.voiceVolume)
                && masterMute == other.masterMute
                && bgmMute == other.bgmMute
                && effectMute == other.effectMute
                && voiceMute == other.voiceMute;
        }

        public override bool Equals(object obj) {
            return obj is AudioSettingModel other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(
                masterVolume, bgmVolume, effectVolume, voiceVolume,
                masterMute, bgmMute, effectMute, voiceMute);
        }
    }
}
