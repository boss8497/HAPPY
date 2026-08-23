using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Script.GUI.Screen.Enum;
using Script.GUI.Screen.Interface;
using Script.GUI.ScreenData.Interface;
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

        // ※ 이 파일의 _screens/_loadedScreens/_firstScreen/_openWaitQueue/_closeWaitQueue와
        // ScreenManager.Loading.cs/SafeArea.cs/StageTransition.cs의 오버레이 필드는
        // ScreenManagerDebugWindow.cs(Tools/Debug/Screen Manager)가 리플렉션으로 직접 참조한다.
        // 이름을 바꾸거나 구조를 바꾸면 그 파일의 FieldInfo 캐시도 같이 확인할 것 (Editor/README.md 체크리스트 참고).
        private ScreenLayer[]                   _layers = new ScreenLayer[(int)ScreenLayerType.Max];
        private Dictionary<string, ScreenAsset> _screens;

        // 이미 로드한 Screen을 가지고 있다가
        // 적절한 타이밍( Scene 이동 )에 Destroy 해주기
        // 모두 다 들고 있으면 메모리 사용량이 컨텐츠를 진행할 때 마다 커짐
        private Dictionary<string, IScreen> _loadedScreens = new();

        private Queue<string> _openWaitQueue  = new Queue<string>();
        private Queue<string> _closeWaitQueue = new Queue<string>();

        private IScreen _firstScreen;


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

                if (layerType == ScreenLayerType.StageTransition) {
                    SetupStageTransitionLayer(obj);
                }

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

            // Inject
            var lastChildScope = _scopeLocator.GetLastChildScope();
            var instanceObj    = lastChildScope.Container.Instantiate(obj);
            instanceObj.SetActive(false);

            Addressables.Release(handle);
            return instanceObj;
        }

        private void InjectGameObject(GameObject obj) {
            var lastChildScope = _scopeLocator.GetLastChildScope();
            lastChildScope.Container.Inject(obj);
        }

        /// <summary>
        /// Screen을 Open하는 메서드입니다.
        /// await으로 기다리면 오픈까지 확실히 기다려 줍니다.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public async UniTask<IScreen> OpenAsync(string key, CancellationToken ct = default) {
            return await OpenAsync(null, key, ct);
        }

        public async UniTask<IScreen> OpenAsync(IScreenOption screenOption, string key, CancellationToken ct = default) {
            if (string.IsNullOrEmpty(key)) {
                Debug.LogError("Screen ID cannot be null or empty");
                return null;
            }

            if (ExistsScreen(key, out var openedScreen)) {
                // 이미 열려있으면 데이터 교체
                if (screenOption != null) {
                    await openedScreen.OpenChangeOptionAsync(screenOption, ct);
                }
                return openedScreen;
            }

            _openWaitQueue.Enqueue(key);
            var isCancel = await UniTask.WaitUntil(() => (OpeningScreen == false && _openWaitQueue.Peek() == key)
                                                       ||
                                                         // 앞선 객체가 cancel되어 뒤에 자동으로 Cancel 됐을 때 무한 대기 방지
                                                         _openWaitQueue.Contains(key) == false
                                                 , cancellationToken: ct)
                                        .SuppressCancellationThrow();

            await ShowSafeAreaAsync();
            AddState(ScreenManagerState.OpeningScreen);

            // Cancel이 되어버려 열리지 못한 Child들 return해주기
            if (_openWaitQueue.Contains(key) == false) {
                await HideSafeAreaAsync();
                RemoveState(ScreenManagerState.OpeningScreen);
                return null;
            }

            // 만약 Cancel로 중지 됐다면 하위에 요청한거 까지 다 지워주자
            if (isCancel) {
                if (_openWaitQueue.Contains(key)) {
                    while (_openWaitQueue.Peek() != key) {
                        _openWaitQueue.Dequeue();
                    }

                    _openWaitQueue.Dequeue();
                }

                await HideSafeAreaAsync();
                RemoveState(ScreenManagerState.OpeningScreen);
                return null;
            }

            var screenKey = _openWaitQueue.Dequeue();
            if (_screens.TryGetValue(screenKey, out ScreenAsset screenAsset) == false) {
                
                await HideSafeAreaAsync();
                RemoveState(ScreenManagerState.OpeningScreen);
                Debug.LogError($"Screen ID {screenKey} not found");
                return null;
            }

            // 이미 로드된 Screen인지 확인
            if (_loadedScreens.TryGetValue(screenKey, out var screenScript)) {
                // 재사용 시 다시 Inject
                InjectGameObject(screenScript.GameObject);
            }
            else {
                var obj = await LoadScreen(screenAsset.screen);
                screenScript = obj.GetComponent<IScreen>();
                if (screenScript == null) {
                    Destroy(obj);
                    await HideSafeAreaAsync();
                    RemoveState(ScreenManagerState.OpeningScreen);
                    Debug.LogError($"Screen Script {screenKey} not found");
                    return null;
                }

                _loadedScreens.Add(screenKey, screenScript);
            }
            
            InsertScreen(screenScript);

            var layer = _layers[(int)screenScript.LayerType];
            await layer.OpenScreen(screenScript, screenOption, ct);
            
            await HideSafeAreaAsync();
            RemoveState(ScreenManagerState.OpeningScreen);
            return screenScript;
        }

        private void InsertScreen(IScreen screen) {
            if (screen == null) {
                Debug.LogError($"Screen cannot be null");
                return;
            }

            // 혹시 이전 링크가 남아 있으면 초기화
            screen.Previous = null;
            screen.Next     = null;

            if (_firstScreen == null) {
                _firstScreen = screen;
                return;
            }

            if (screen.DontClose) {
                InsertDontCloseScreen(screen);
                return;
            }

            InsertNormalScreen(screen);
        }

        private void InsertDontCloseScreen(IScreen screen) {
            // 첫 화면이 일반 Screen이면 맨 앞에 삽입
            if (_firstScreen.DontClose == false) {
                screen.Next           = _firstScreen;
                _firstScreen.Previous = screen;
                _firstScreen          = screen;
                return;
            }

            // 마지막 DontClose 뒤에 삽입
            var current = _firstScreen;
            while (current.Next != null && current.Next.DontClose) {
                current = current.Next;
            }

            screen.Next = current.Next;
            if (current.Next != null) {
                current.Next.Previous = screen;
            }

            current.Next    = screen;
            screen.Previous = current;
        }

        private void InsertNormalScreen(IScreen screen) {
            var last = LastScreen();
            last.Next       = screen;
            screen.Previous = last;
        }

        private void DetachScreen(IScreen screen) {
            if (screen == null) {
                return;
            }

            var previous = screen.Previous;
            var next     = screen.Next;

            if (previous != null) {
                previous.Next = next;
            }
            else if (ReferenceEquals(_firstScreen, screen)) {
                _firstScreen = next;
            }

            if (next != null) {
                next.Previous = previous;
            }

            screen.Previous = null;
            screen.Next     = null;
        }

        public async UniTask CloseAllAsync() {
            var lastScreen = LastScreen();
            while (lastScreen != null) {
                await CloseAsync(_firstScreen, true);
                lastScreen = LastScreen();
            }
        }

        /// <summary>
        /// Screen을 하나씩 Back하는 메서드입니다. CloseAsync 보다는 이거를 적극 사용!
        /// </summary>
        public async UniTask BackAsync(bool force = false, CancellationToken ct = default) {
            var screen = BackScreen();
            screen.AddState(ScreenState.Closing);
            Debug.Log($"Back Screen {screen.Key}");
            await CloseAsync(screen, force, ct);
        }

        /// <summary>
        /// Screen을 지정해서 Close하는 메서드입니다.
        /// </summary>
        /// <para>Close는 특별하게 사용하고 대부분 Back을 사용하는게 좋습니다.</para>
        public async UniTask CloseAsync(ReadOnlyMemory<char> key, bool force = false, CancellationToken ct = default) {
            var screen = FindScreen(key.Span);
            if (screen == null) {
                Debug.LogError($"Screen ID {key.ToString()} not found");
                return;
            }

            await CloseAsync(screen, force, ct);
        }

        /// <summary>
        /// Screen을 지정해서 Close하는 메서드입니다.
        /// </summary>
        /// <para>Close는 특별하게 사용하고 대부분 Back을 사용하는게 좋습니다.</para>
        public async UniTask CloseAsync(IScreen screen, bool force = false, CancellationToken ct = default) {
            if (screen == null) {
                return;
            }

            // 같은 Key로 중복 요청이 들어와도 큐에 그대로 쌓아서 순서대로 처리한다.
            // (여기서 "이미 대기 중이면 무시하고 return" 식으로 막으면, 이 return은 await 없이 동기적으로
            //  끝나버려서 CloseAllAsync() 같은 while 루프가 프레임 양보 없이 계속 재호출 -> 무한 스핀에 빠진다.
            //  뒤에서 FindScreen()==null 체크로 "대기 중 이미 닫힌 경우"를 안전하게 처리하므로 별도 가드가 필요 없다.)
            _closeWaitQueue.Enqueue(screen.Key);

            await UniTask.WaitUntil(() => ClosingScreen == false && _closeWaitQueue.Peek().AsSpan().SequenceEqual(screen.Key.AsSpan()), cancellationToken:ct);

            AddState(ScreenManagerState.ClosingScreen);

            try {
                var screenKey = _closeWaitQueue.Dequeue();

                // 대기 중 이미 닫혔을 수 있음
                var current = FindScreen(screenKey.AsSpan());
                if (current == null)
                    return;

                var trigger = await current.CloseTrigger();
                if (trigger == false) return;

                var targets = ListPool.Get<IScreen>();
                
                // 해당 상황이 안나오게 Close를 직접 호출하지 않는게 중요
                CollectCloseTargets(current, targets);

                foreach (var target in targets) {
                    if (force == false && target.DontClose) {
                        continue;
                    }
                    
                    // 리스트에서 먼저 제거
                    DetachScreen(target);

                    var layer = _layers[(int)target.LayerType];

                    // DontClose는 명시적으로 닫을 때만 force
                    await layer.CloseScreen(target, force);
                    
                    target.AddState(ScreenState.Closed);
                    target.RemoveState(ScreenState.Closing);
                }

                targets.Clear();
                ListPool.Return(targets);
            }
            finally {
                RemoveState(ScreenManagerState.ClosingScreen);
            }
        }

        private void CollectCloseTargets(IScreen screen, List<IScreen> targets) {
            if (screen == null) {
                return;
            }

            // DontClose는 자기 자신만 닫음
            if (screen.DontClose) {
                targets.Add(screen);
                return;
            }

            // 일반 Screen은 자신 ~ tail 까지 닫되,
            // 실제 Close 순서는 tail -> ... -> self
            var current = LastScreen(screen);
            while (current != null) {
                targets.Add(current);

                if (ReferenceEquals(current, screen)) {
                    break;
                }

                current = current.Previous;
            }
        }
        
        private IScreen BackScreen() {
            var lastScreen = _firstScreen;
            while (lastScreen?.Next != null) {
                lastScreen = lastScreen?.Next;
            }

            var backScreen = lastScreen;
            while (backScreen?.ClosingScreen ?? false) {
                backScreen = backScreen.Previous;
            }

            return backScreen;
        }

        private IScreen LastScreen() {
            var lastScreen = _firstScreen;
            while (lastScreen?.Next != null) {
                lastScreen = lastScreen?.Next;
            }

            return lastScreen;
        }

        private IScreen LastScreen(IScreen screen) {
            var lastScreen = screen;
            while (lastScreen.Next != null) {
                lastScreen = lastScreen.Next;
            }

            return lastScreen;
        }

        [CanBeNull]
        private IScreen FindScreen(ReadOnlySpan<char> key) {
            var currentScreen = _firstScreen;

            while (currentScreen != null) {
                if (currentScreen.Key.AsSpan().SequenceEqual(key)) {
                    return currentScreen;
                }

                currentScreen = currentScreen.Next;
            }

            return null;
        }

        private bool ExistsScreen(string key, out IScreen screen) {
            screen = _firstScreen;
            while (screen != null) {
                if (screen.Key == key) {
                    return true;
                }

                screen = screen.Next;
            }
            return false;
        }

        public async UniTask ResourceClear() {
            _uiPooling.Clear();
            foreach (var screen in _loadedScreens.Select(i => i.Value).ToArray()) {
                await screen.Release();
                Destroy(screen.GameObject);
            }

            _loadedScreens.Clear();
        }
    }
}