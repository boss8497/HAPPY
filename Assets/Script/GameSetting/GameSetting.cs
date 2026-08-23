using System;
using Cysharp.Threading.Tasks;
using Script.Addressable;
using Script.GameSetting.Data;
using Script.GameSetting.Interface;
using UnityEngine;
using VContainer.Unity;

namespace Script.GameSetting {
    public class GameSetting : IGameSetting, IInitializable {
        private readonly string _gameSettingKey = nameof(GameSettingAsset);

        private readonly IAddressableService _addressableService;

        public GameSettingData GameSettingData { get; private set; }
        public bool            Initialized     { get; private set; }

        public GameSetting(IAddressableService addressableService) {
            _addressableService = addressableService ?? throw new ArgumentNullException(nameof(addressableService));
        }

        public void Initialize() {
        }

        public void InitializeGameSetting() {
            InitializeGameSettingAsync().Forget();
        }

        private async UniTask InitializeGameSettingAsync() {
            await LoadGameSettingData();
            InitializeFrameRate();
            Initialized = true;
        }

        private async UniTask LoadGameSettingData() {
            using var handle = await _addressableService.LoadAsync<GameSettingAsset>(_gameSettingKey);
            GameSettingData = handle.Value.gameSettingData;

            _addressableService.ConfigureCache(
                GameSettingData.addressableCacheCheckIntervalSeconds,
                GameSettingData.addressableCacheReleaseGraceSeconds
            );
        }

        private void InitializeFrameRate() {
            Application.targetFrameRate = GameSettingData.frameRate;
            QualitySettings.vSyncCount  = GameSettingData.vSyncCount;
        }
    }
}