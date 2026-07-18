using Cysharp.Threading.Tasks;
using Script.GamePlay.Audio.Interface;
using UnityEngine;

namespace Script.GamePlay.Audio {
    public partial class AudioManager {
        private const float MinDecibel = -80f;

        public float GetVolume(AudioGroupType group) => _audioSetting.GetVolume(group);

        public void SetVolume(AudioGroupType group, float volume01) {
            _audioSetting.SetVolume(group, volume01);
            ApplyVolume(group);
            _audioSetting.SaveAsync().Forget();
        }

        public bool GetMute(AudioGroupType group) => _audioSetting.GetMute(group);

        public void SetMute(AudioGroupType group, bool mute) {
            _audioSetting.SetMute(group, mute);
            ApplyVolume(group);
            _audioSetting.SaveAsync().Forget();
        }

        private void ApplyVolume(AudioGroupType group) {
            if (_mixerGroups.ContainsKey(group) == false)
                return;

            var mute   = _audioSetting.GetMute(group);
            var volume = mute ? 0f : _audioSetting.GetVolume(group);

            _mixer.SetFloat($"{group}Volume", LinearToDecibel(volume));
        }

        private static float LinearToDecibel(float linear) {
            return linear <= 0.0001f ? MinDecibel : Mathf.Log10(linear) * 20f;
        }
    }
}
