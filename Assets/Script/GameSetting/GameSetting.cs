using System;
using Script.GameSetting.Data;
using Script.GameSetting.Interface;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

namespace Script.GameSetting {
    public class GameSetting : IGameSetting, IInitializable {
        private readonly string _gameSettingKey = nameof(GameSettingAsset);

        public GameSettingData GameSettingData { get; private set; }
        public bool            Initialized     { get; private set; }


        public string TitleScenePath   => GameSettingData.titleScenePath;
        public string StartUpScenePath => GameSettingData.startUpScenePath;
        public string LobbyScenePath   => GameSettingData.lobbyScenePath;

        public void Initialize() {
            LoadGameSettingData();
            InitializeGameSetting();
            Initialized = true;
        }

        private void LoadGameSettingData() {
            var handle = Addressables.LoadAssetAsync<GameSettingAsset>(_gameSettingKey);
            handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Load failed: {_gameSettingKey}");

            GameSettingData = handle.Result.gameSettingData;
            Addressables.Release(handle);
        }

        private void InitializeGameSetting() {
            Application.targetFrameRate = GameSettingData.frameRate;
            QualitySettings.vSyncCount  = GameSettingData.vSyncCount;
        }
    }
}