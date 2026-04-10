using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;
using Script.GUI.Screen.Interface;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

//Lifetime
//StartUpScene에서 생성되기 때문에 한번만 생성 되고 계속 유지.

namespace Script.GUI.Screen {
    public partial class ScreenManager : MonoBehaviour, IScreenManager {
        private readonly string _screenDataPath = nameof(ScreenData);

        [SerializeField]
        private RectTransform layerParent;

        private RectTransform[]                 _layers = new RectTransform[(int)ScreenLayer.Max];
        private Dictionary<string, ScreenAsset> _screens;

        public void Initialize() {
            DontDestroyOnLoad(this);
            CreateLayer();
            LoadScreenData();
            AddState(ScreenManagerState.Initialized);
        }

        private void CreateLayer() {
            for (int i = 0; i < (int)ScreenLayer.Max; ++i) {
                var layer = (ScreenLayer)i;

                var obj  = new GameObject(layer.ToString(), typeof(RectTransform));
                var rect = obj.GetComponent<RectTransform>();

                rect.SetParent(layerParent, false);

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;

                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private void LoadScreenData() {
            var handle = Addressables.LoadAssetAsync<ScreenData>(_screenDataPath);
            handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Load failed: {_screenDataPath}");

            _screens = handle.Result.screens.ToDictionary(r => r.id, r => r);

            Addressables.Release(handle);
        }

        public UniTask OpenScreen(string id) {
            Debug.Log($"Opening screen call : {id}");
            return UniTask.CompletedTask;
        }
    }
}