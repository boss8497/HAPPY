using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.GamePlay.Audio.Interface;
using UnityEngine;

namespace Script.GamePlay.Audio {
    public partial class AudioManager {
        private const float MinDecibel = -80f;

        public float GetVolume(AudioGroup group) => _audioSetting.GetVolume(group);

        public void SetVolume(AudioGroup group, float volume01, bool save = true) {
            _audioSetting.SetVolume(group, volume01);
            ApplyVolume(group);
            if (save) {
                _audioSetting.SaveAsync().Forget();
            }
        }

        public bool GetMute(AudioGroup group) => _audioSetting.GetMute(group);

        public void SetMute(AudioGroup group, bool mute, bool save = true) {
            _audioSetting.SetMute(group, mute);
            ApplyVolume(group);
            if (save) {
                _audioSetting.SaveAsync().Forget();
            }
        }

        private void ApplyVolume(AudioGroup group) {
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