using System;
using Cysharp.Threading.Tasks;
using Script.DataBase.Enum;
using Script.DataBase.Interface;
using Script.GameInfo.Info;
using Script.GamePlay.Audio.Interface;
using Script.GamePlay.Audio.Model;

namespace Script.GamePlay.Audio {
    /// <summary>
    /// AudioSettingModel의 로드/저장 + 그룹별 접근을 담당.
    /// AudioManager가 소유하는 순수 C# 헬퍼로, 별도 DI 등록 없이 생성자에서 직접 생성한다.
    /// </summary>
    public class AudioSetting {
        private static readonly string SettingPath = $"{nameof(AudioSettingModel)}.json";

        private readonly IDataBase _dataBase;

        public AudioSettingModel Model { get; private set; }

        public AudioSetting(IDataBase dataBase) {
            _dataBase = dataBase ?? throw new ArgumentNullException(nameof(dataBase));
        }

        public async UniTask LoadAsync() {
            if (_dataBase.Exists(SettingPath)) {
                Model = await _dataBase.LoadAsync<AudioSettingModel>(SettingPath, DataType.Json);
                return;
            }

            Model = AudioSettingModel.CreateDefault();
            await SaveAsync();
        }

        public UniTask SaveAsync() {
            return _dataBase.SaveAsync(SettingPath, Model, DataType.Json);
        }

        public float GetVolume(AudioGroup group) {
            return group switch {
                AudioGroup.Master => Model.masterVolume,
                AudioGroup.BGM    => Model.bgmVolume,
                AudioGroup.Effect => Model.effectVolume,
                AudioGroup.Voice  => Model.voiceVolume,
                _                     => throw new ArgumentOutOfRangeException(nameof(group), group, null),
            };
        }

        public void SetVolume(AudioGroup group, float volume01) {
            var clamped = UnityEngine.Mathf.Clamp01(volume01);

            switch (group) {
                case AudioGroup.Master:
                    Model.masterVolume = clamped;
                    break;
                case AudioGroup.BGM:
                    Model.bgmVolume = clamped;
                    break;
                case AudioGroup.Effect:
                    Model.effectVolume = clamped;
                    break;
                case AudioGroup.Voice:
                    Model.voiceVolume = clamped;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(group), group, null);
            }
        }

        public bool GetMute(AudioGroup group) {
            return group switch {
                AudioGroup.Master => Model.masterMute,
                AudioGroup.BGM    => Model.bgmMute,
                AudioGroup.Effect => Model.effectMute,
                AudioGroup.Voice  => Model.voiceMute,
                _                     => throw new ArgumentOutOfRangeException(nameof(group), group, null),
            };
        }

        public void SetMute(AudioGroup group, bool mute) {
            switch (group) {
                case AudioGroup.Master:
                    Model.masterMute = mute;
                    break;
                case AudioGroup.BGM:
                    Model.bgmMute = mute;
                    break;
                case AudioGroup.Effect:
                    Model.effectMute = mute;
                    break;
                case AudioGroup.Voice:
                    Model.voiceMute = mute;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(group), group, null);
            }
        }
    }
}
