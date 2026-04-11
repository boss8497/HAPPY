using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;
using Script.GUI.Screen.Interface;
using Script.Utility.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;


namespace Script.GUI.Screen {
    /// <summary>
    /// ScreenManager는 UI Screen을 관리하는 매니저입니다.
    /// </summary>
    /// <remarks>
    /// <para>생성 위치 : AppLifetimeScope(Root)에서 Asset을 불러와 생성</para>
    /// <para>Preload : 하지 않는다. 무거운 UI는 미리 Preload해서 사용하면 좋지만, 이미 무겁다는게 최적화가 안됐다고 생각</para>
    /// </remarks>
    public partial class ScreenManager : MonoBehaviour, IScreenManager {
        private readonly string _screenDataPath = nameof(ScreenData);

        [SerializeField]
        private RectTransform layerParent;

        private ScreenLayer[]                   _layers = new ScreenLayer[(int)ScreenLayerType.Max];
        private Dictionary<string, ScreenAsset> _screens;
        // 이미 로드한 Screen을 가지고 있다가
        // 적절한 타이밍( Scene 이동 )에 Destroy 해주기
        // 모두 다 들고 있으면 메모리 사용량이 컨텐츠를 진행할 때 마다 커짐
        private Dictionary<string, Screen>  _loadedScreens = new();

        private Screen _firstScreen;


        public void Initialize() {
            DontDestroyOnLoad(this);
            CreateLayer();
            LoadScreenData();
            AddState(ScreenManagerState.Initialized);
        }

        private void CreateLayer() {
            for (int i = 0; i < (int)ScreenLayerType.Max; ++i) {
                var layerType = (ScreenLayerType)i;

                var obj  = new GameObject(layerType.ToString(), typeof(RectTransform));
                var rect = obj.GetComponent<RectTransform>();

                rect.SetParent(layerParent, false);

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;

                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var layer = new ScreenLayer(this, rect);
                _layers[i] = layer;
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

        private async UniTask<GameObject> LoadScreen(AssetReferenceT<GameObject> assetRef) {
            var handle = Addressables.LoadAssetAsync<GameObject>(assetRef.RuntimeKey);
            var obj    = await handle.ToUniTask();
            obj.SetActiveSafe(false);
            var instanceObj = _resolver.Instantiate(obj);

            Addressables.Release(handle);
            return instanceObj;
        }

        public async UniTask OpenScreen(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException("Screen ID cannot be null or empty");
            }

            if (_screens.TryGetValue(id, out ScreenAsset screen) == false) {
                throw new KeyNotFoundException($"Screen ID {id} not found");
            }

            // 이미 로드된 Screen인지 확인
            if (_loadedScreens.TryGetValue(id, out var screenScript) == false) {
                var obj          = await LoadScreen(screen.screen);
                screenScript = obj.GetComponent<Screen>();
                if (screenScript == null) {
                    Destroy(obj);
                    throw new KeyNotFoundException($"Screen Script {id} not found");
                }
                
                _loadedScreens.Add(id, screenScript);
            }

            InsertScreen(screenScript);

            var layer = _layers[(int)screenScript.LayerType];
            await layer.OpenScreen(screenScript);
        }

        private void InsertScreen(Screen screen) {
            if (screen.DontClose) {
                if (_firstScreen == null) {
                    _firstScreen = screen;
                }
                else {
                    if (_firstScreen.DontClose == false) {
                        screen.Next           = _firstScreen;
                        _firstScreen.Previous = screen;
                        
                        _firstScreen =  screen;
                    }
                    else {
                        var nextScreen = _firstScreen.Next;
                        while (nextScreen.DontClose) {
                            nextScreen = nextScreen.Next;
                        }

                        screen.Next     = nextScreen.Next; 
                        nextScreen.Next = screen;
                        screen.Previous = nextScreen;
                    }
                }
            }
            else {
                if (_firstScreen == null) {
                    _firstScreen = screen;
                }
                else {
                    var nextScreen = _firstScreen.Next;
                    while (nextScreen != null) {
                        nextScreen = nextScreen.Next;
                    }

                    nextScreen.Next = screen;
                    screen.Previous = nextScreen;
                }
            }
        }

        public async UniTask CloseAllAsync() { }

        
        public async UniTask LoadedScreenRelease() {
            foreach (var screen in _loadedScreens.Select(i => i.Value).ToArray()) {
                await screen.Release();
                Destroy(screen.gameObject);
            }
            _loadedScreens.Clear();
        }
    }
}